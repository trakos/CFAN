using System;
using System.Collections.Generic;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Version;
using NUnit.Framework;
using Tests.Data;
using log4net;

namespace Tests.Core.Types
{
    [TestFixture]
    public class GameComparator
    {
        private static readonly FactorioVersion GameVersion = new FactorioVersion("0.12.5");
        private static readonly RandomModuleGenerator RandomModuleGenerator = new RandomModuleGenerator(new Random());
        CfanModule gameMod;

        [Test]
        [TestCase(typeof(CKAN.StrictGameComparator), true)]
        [TestCase(typeof(CKAN.GrasGameComparator), true)]
        [TestCase(typeof(CKAN.YoyoGameComparator), true)]
        public void TotallyCompatible(Type type, bool expected)
        {
            var comparator = (CKAN.IGameComparator) Activator.CreateInstance(type);
            gameMod = RandomModuleGenerator.GeneratorRandomModule(depends: new List<ModDependency> {new ModDependency("base==0.12.5")});

            // Now test!
            Assert.AreEqual(expected, comparator.Compatible(GameVersion, gameMod));
        }

        [Test]
        [TestCase(typeof(CKAN.StrictGameComparator), false)]
        [TestCase(typeof(CKAN.GrasGameComparator), false)]
        [TestCase(typeof(CKAN.YoyoGameComparator), true)]
        public void GenerallySafeStrict(Type type, bool expected)
        {
            var comparator = (CKAN.IGameComparator) Activator.CreateInstance(type);
            gameMod = RandomModuleGenerator.GeneratorRandomModule(depends: new List<ModDependency> { new ModDependency("base==0.12.4") });

            // Now test!
            Assert.AreEqual(expected, comparator.Compatible(GameVersion, gameMod));
        }

        [Test]
        [TestCase(typeof(CKAN.StrictGameComparator), true)]
        [TestCase(typeof(CKAN.GrasGameComparator), true)]
        [TestCase(typeof(CKAN.YoyoGameComparator), true)]
        public void Incompatible(Type type, bool expected)
        {
            var comparator = (CKAN.IGameComparator) Activator.CreateInstance(type);
            gameMod = RandomModuleGenerator.GeneratorRandomModule(depends: new List<ModDependency> { new ModDependency("something") });

            // The mod without any version restriction is compatible with everything
            Assert.AreEqual(expected, comparator.Compatible(GameVersion, gameMod));
        }
    }
}

