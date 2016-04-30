using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;

namespace CKAN
{
    public enum RelationshipType
    {
        Depends = 0,
        Recommends = 1,
        Suggests = 2,
        Supports = 3,
        Conflicts = 4
    }

    public partial class Main : Form
    {
        private BackgroundWorker m_CacheWorker;

        private void UpdateModInfo(GUIMod gui_module)
        {
            CfanModule module = gui_module.ToModule();

            Util.Invoke(MetadataModuleNameLabel, () => MetadataModuleNameLabel.Text = gui_module.Name);
            Util.Invoke(MetadataModuleVersionLabel, () => MetadataModuleVersionLabel.Text = gui_module.LatestVersion.ToString());
            Util.Invoke(MetadataModuleLicenseLabel, () => MetadataModuleLicenseLabel.Text = "");
            Util.Invoke(MetadataModuleAuthorLabel, () => MetadataModuleAuthorLabel.Text = gui_module.Authors);
            Util.Invoke(MetadataModuleAbstractLabel, () => MetadataModuleAbstractLabel.Text = module.@abstract);
            Util.Invoke(MetadataIdentifierLabel, () => MetadataIdentifierLabel.Text = module.identifier);

            // If we have homepage provided use that, otherwise use the spacedock page or the github repo so that users have somewhere to get more info than just the abstract.
            Util.Invoke(MetadataModuleHomePageLinkLabel,
                       () => MetadataModuleHomePageLinkLabel.Text = gui_module.Homepage.ToString());

            if (string.IsNullOrEmpty(gui_module.Homepage))
            {
                Util.Invoke(MetadataModuleGitHubLinkLabel,
                    () => MetadataModuleGitHubLinkLabel.Text = "N/A");
            }

            if (module.release_status != null)
            {
                Util.Invoke(MetadataModuleReleaseStatusLabel, () => MetadataModuleReleaseStatusLabel.Text = module.release_status.ToString());
            }

            Util.Invoke(MetadataModuleKSPCompatibilityLabel, () => MetadataModuleKSPCompatibilityLabel.Text = gui_module.KSPCompatibilityLong);
        }

        private HashSet<CfanModule> alreadyVisited = new HashSet<CfanModule>();

        private TreeNode UpdateModDependencyGraphRecursively(TreeNode parentNode, CfanModule module, RelationshipType relationship, int depth, bool virtualProvides = false)
        {
            if (module == null
                || (depth > 0 && dependencyGraphRootModule == module)
                || (alreadyVisited.Contains(module)))
            {
                return null;
            }

            alreadyVisited.Add(module);

            string nodeText = module.title;
            if (virtualProvides)
            {
                nodeText = String.Format("provided by - {0}", module.title);
            }

            var node = parentNode == null ? new TreeNode(nodeText) : parentNode.Nodes.Add(nodeText);
            node.Name = module.title;

            IEnumerable<ModDependency> relationships = null;
            switch (relationship)
            {
                case RelationshipType.Depends:
                    relationships = module.depends;
                    break;
                case RelationshipType.Recommends:
                    relationships = module.recommends;
                    break;
                case RelationshipType.Suggests:
                    relationships = module.suggests;
                    break;
                case RelationshipType.Supports:
                    relationships = module.supports;
                    break;
                case RelationshipType.Conflicts:
                    relationships = module.conflicts;
                    break;
            }

            if (relationships == null)
            {
                return node;
            }

            foreach (ModDependency dependency in relationships)
            {
                IRegistryQuerier registry = RegistryManager.Instance(manager.CurrentInstance).registry;

                try
                {
                    try
                    {
                        var dependencyModule = registry.LatestAvailable
                            (dependency.modName, manager.CurrentInstance.Version());
                        UpdateModDependencyGraphRecursively(node, dependencyModule, relationship, depth + 1);
                    }
                    catch (ModuleNotFoundKraken)
                    {
                        List<CfanModule> dependencyModules = registry.LatestAvailableWithProvides
                            (dependency.modName, manager.CurrentInstance.Version());

                        if (dependencyModules == null)
                        {
                            continue;
                        }

                        var newNode = node.Nodes.Add(dependency.modName + " (virtual)");
                        newNode.ForeColor = Color.Gray;

                        foreach (var dep in dependencyModules)
                        {
                            UpdateModDependencyGraphRecursively(newNode, dep, relationship, depth + 1, true);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            if (virtualProvides)
            {
                node.Collapse(true);
            }
            else
            {
                node.ExpandAll();
            }

            return node;
        }

        private void UpdateModDependencyGraph(CfanModule module)
        {
            ModInfoTabControl.Tag = module ?? ModInfoTabControl.Tag;
            //Can be costly. For now only update when visible.
            if (ModInfoTabControl.SelectedIndex != RelationshipTabPage.TabIndex)
            {
                return;
            }
            Util.Invoke(DependsGraphTree, _UpdateModDependencyGraph);
        }

        private CfanModule dependencyGraphRootModule;

        private void _UpdateModDependencyGraph()
        {
            var module = (CfanModule) ModInfoTabControl.Tag;
            dependencyGraphRootModule = module;


            if (ModuleRelationshipType.SelectedIndex == -1)
            {
                ModuleRelationshipType.SelectedIndex = 0;
            }

            var relationshipType = (RelationshipType) ModuleRelationshipType.SelectedIndex;


            alreadyVisited.Clear();

            DependsGraphTree.Nodes.Clear();
            DependsGraphTree.Nodes.Add(UpdateModDependencyGraphRecursively(null, module, relationshipType, 0));
        }

        // When switching tabs ensure that the resulting tab is updated.
        private void ModInfoIndexChanged(object sender, EventArgs e)
        {
            if (ModInfoTabControl.SelectedIndex == ContentTabPage.TabIndex)
                UpdateModContentsTree(null);
            if (ModInfoTabControl.SelectedIndex == RelationshipTabPage.TabIndex)
                UpdateModDependencyGraph(null);
        }

        private void UpdateModContentsTree(CfanModule module, bool force = false)
        {
            ModInfoTabControl.Tag = module ?? ModInfoTabControl.Tag;
            //Can be costly. For now only update when visible.
            if (ModInfoTabControl.SelectedIndex != ContentTabPage.TabIndex && !force)
            {
                return;
            }
            Util.Invoke(ContentsPreviewTree, () => _UpdateModContentsTree(force));
        }

        private CfanModule current_mod_contents_module;

        private void _UpdateModContentsTree(bool force = false)
        {
            GUIMod guiMod = GetSelectedModule();
            if (!guiMod.IsCKAN)
            {
                return;
            }
            CfanModule module = guiMod.ToCkanModule();
            if (Equals(module, current_mod_contents_module) && !force)
            {
                return;
            }
            else
            {
                current_mod_contents_module = module;
            }
            if (!guiMod.IsCached)
            {
                NotCachedLabel.Text = "This mod is not in the cache, click 'Download' to preview contents";
                ContentsDownloadButton.Enabled = true;
                ContentsPreviewTree.Enabled = false;
            }
            else
            {
                NotCachedLabel.Text = "Module is cached, preview available";
                ContentsDownloadButton.Enabled = false;
                ContentsPreviewTree.Enabled = true;
            }

            ContentsPreviewTree.Nodes.Clear();
            ContentsPreviewTree.Nodes.Add(module.title);

            IEnumerable<string> contents = ModuleInstaller.GetInstance(manager.CurrentInstance, GUI.user).GetModuleContentsList(module);
            if (contents == null)
            {
                return;
            }

            foreach (string item in contents)
            {
                ContentsPreviewTree.Nodes[0].Nodes.Add(item);
            }

            ContentsPreviewTree.Nodes[0].ExpandAll();
        }

        private void CacheMod(object sender, DoWorkEventArgs e)
        {
            CfanModule module = (CfanModule) e.Argument;
            if (module.isMetapackage)
            {
                AddLogMessage($"Cannot download metapackage. {module.title}");
                return;
            }
            if (module.download == null)
            {
                AddLogMessage($"No available download link for {module.title}.");
                return;
            }
            ResetProgress();
            ClearLog();

            NetAsyncModulesDownloader dowloader = new NetAsyncModulesDownloader(m_User);
            
            dowloader.DownloadModules(CurrentInstance.Cache, new List<CfanModule> { module });
            e.Result = e.Argument;
        }

        private void PostModCaching(object sender, RunWorkerCompletedEventArgs e)
        {
            Util.Invoke(this, () => _PostModCaching((CfanModule)e.Result));
        }

        private void _PostModCaching(CfanModule module)
        {
            HideWaitDialog(true);

            UpdateModContentsTree(module, true);
            RecreateDialogs();
        }

        /// <summary>
        /// Opens the file browser of the users system
        /// with the folder of the clicked node opened
        /// TODO: Open a file broweser with the file selected
        /// </summary>
        /// <param name="node">A node of the ContentsPreviewTree</param>
        internal void OpenFileBrowser(TreeNode node)
        {
            string location = node.Text;

            if (File.Exists(location))
            {
                //We need the Folder of the file
                //Otherwise the OS would try to open the file in it's default application
                location = Path.GetDirectoryName(location);
            }

            if (!Directory.Exists(location))
            {
                //User either selected the parent node
                //or he clicked on the tree node of a cached, but not installed mod
                return;
            }

            Process.Start(location);
        }
    }
}