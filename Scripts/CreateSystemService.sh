scp Azure-DDNS-Updater.service pi@octopi:/home/pi/

dotnet publish -r linux-arm
cd bin\Debug\netcore3.0\linux-arm\publish
scp -r . pi@octopi:/home/pi/Azure-DDNS-Updater

ssh pi@octopi
sudo apt-get install curl libunwind8 gettext
wget https://download.visualstudio.microsoft.com/download/pr/8ddb8193-f88c-4c4b-82a3-39fcced27e91/b8e0b9bf4cf77dff09ff86cc1a73960b/dotnet-sdk-3.0.100-linux-arm.tar.gz
wget https://download.visualstudio.microsoft.com/download/pr/e9d4b012-a877-443c-8344-72ef910c86dd/b5e729b532d7b3b5488c97764bd0fb8e/aspnetcore-runtime-3.0.0-linux-arm.tar.gz
mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-3.0.100-linux-arm.tar.gz -C $HOME/dotnet
export DOTNET_ROOT=$HOME/dotnet 
export PATH=$PATH:$HOME/dotnet
tar zxf aspnetcore-runtime-3.0.0-linux-arm.tar.gz -C $HOME/dotnet

sudo cp Azure-DDNS-Updater.service /etc/systemd/system/Azure-DDNS-Updater.service
sudo chmod 644 /etc/systemd/system/Azure-DDNS-Updater.service
sudo systemctl start Azure-DDNS-Updater
sudo systemctl status Azure-DDNS-Updater
exit
