#!/bin/ash
set -e

DLL_NAME="$1"

CERT_PATH="/defaultcert.pfx"

if [ ! -f "$CERT_PATH" ]; then
  echo "Generating https cert"
  # Generate key and certificate using OpenSSL
  openssl req -x509 -nodes -newkey rsa:2048 \
    -keyout key.pem -out cert.pem \
    -days 3650 \
    -subj "/CN=localhost"
  # Export to PFX format with an empty password
  openssl pkcs12 -export -out "$CERT_PATH" \
    -inkey key.pem -in cert.pem -passout pass:
  # Clean up temporary files
  rm key.pem cert.pem
fi

exec dotnet "$DLL_NAME"
