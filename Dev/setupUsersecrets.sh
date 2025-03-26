cat ./devSecrets.json | dotnet user-secrets -p ../Common/Common.csproj set

hostname=$(hostname)

echo "Enter your local machines IP address / Hostname [$hostname]"
read -r ip

if [ -z "$ip" ]; then
    ip=$hostname
fi

echo "Setting OPENSHOCK:LCG:FQDN to $ip:5443"
dotnet user-secrets -p ../Common/Common.csproj set "OPENSHOCK:LCG:FQDN" "$ip:5443"

