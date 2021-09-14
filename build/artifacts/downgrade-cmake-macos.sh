CMAKE_VERSION=3.19.8
echo "Uninstalling brew CMake"
brew uninstall cmake
echo "Downloading CMake $CMAKE_VERSION"
curl --output cmake-$CMAKE_VERSION-macos-universal.tar.gz -L https://github.com/Kitware/CMake/releases/download/v$CMAKE_VERSION/cmake-$CMAKE_VERSION-macos-universal.tar.gz
echo "Extracting archive"
tar -xzvf cmake-$CMAKE_VERSION-macos-universal.tar.gz
echo "Copying Cmake.app to Applications"
cp -rf ./cmake-$CMAKE_VERSION-macos-universal/CMake.app /Applications
echo "Updating local PATH"
touch ~/.zshrc
grep -qxF 'export PATH=${PATH}:/Applications/CMake.app/Contents/bin' ~/.zshrc || echo 'export PATH=${PATH}:/Applications/CMake.app/Contents/bin' >> ~/.zshrc
source ~/.zshrc
echo "Cleaning up files"
rm -rf ./cmake-$CMAKE_VERSION-macos-universal
rm ./cmake-$CMAKE_VERSION-macos-universal.tar.gz