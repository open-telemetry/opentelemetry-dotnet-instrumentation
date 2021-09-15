curl -sL https://cmake.org/files/v3.19/cmake-3.19.8-Linux-x86_64.sh -o cmakeinstall.sh
chmod +x cmakeinstall.sh
./cmakeinstall.sh --prefix=/usr/local --exclude-subdir
rm cmakeinstall.sh