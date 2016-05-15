#!/usr/bin/env bash

echo "Creating directory..."
curl -X MKCOL -u $WEBDAV_CREDENTIALS "$WEBDAV_URL/$TRAVIS_BUILD_NUMBER"
echo "Uploading cfan.exe..."
curl -X PUT -u $WEBDAV_CREDENTIALS "$WEBDAV_URL/$TRAVIS_BUILD_NUMBER/cfan.exe" -T build/cfan.exe
echo "Uploading cfan_headless.exe..."
curl -X PUT -u $WEBDAV_CREDENTIALS "$WEBDAV_URL/$TRAVIS_BUILD_NUMBER/cfan_headless.exe" -T build/cfan_headless.exe
echo "Uploading cfan_netfan.exe..."
curl -X PUT -u $WEBDAV_CREDENTIALS "$WEBDAV_URL/$TRAVIS_BUILD_NUMBER/cfan_netfan.exe" -T build/cfan_netfan.exe
echo "Uploading cfan_updater.exe..."
curl -X PUT -u $WEBDAV_CREDENTIALS "$WEBDAV_URL/$TRAVIS_BUILD_NUMBER/cfan_updater.exe" -T build/cfan_updater.exe
echo "Webdav deployment complete."