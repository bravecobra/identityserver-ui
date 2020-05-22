# Identity Server UI

This project provides a more complete web interface, but still based on the quick-start UI for [Identity Server 4](https://github.com/IdentityServer/IdentityServer4).

## How to run

### DNS

Edit your hosts file (`C:\Windows\system32\drivers\etc\hosts`) as administrator and add the following entries

```custom
127.0.0.1 localhost.com sts.localhost.com api.localhost.com jsclient.localhost.com mvcclient.localhost.com
```

### Certificates

### Root certificate

Use [mkcert](https://github.com/FiloSottile/mkcert) to generate local self-signed certificates.

> If the domain is publicly available through DNS, you can use Let's Encypt

On windows `mkcert -install` must be executed under elevated Administrator privileges.

```bash
cd compose/nginx/certs
mkcert --install
copy $env:LOCALAPPDATA\mkcert\rootCA.pem ./cacerts.pem
copy $env:LOCALAPPDATA\mkcert\rootCA.pem ./cacerts.crt
```

### Website certificates

Generate a certificate for `sts.localhost.com` with wildcards for the subdomains. The name of the certificate files need to match with actual domainnames in order for the nginx proxy to pick them up correctly.

```bash
cd compose/nginx/certs
mkcert -cert-file localhost.com.crt -key-file localhost.com.key localhost.com *.localhost.com
mkcert -pkcs12 localhost.com.pfx localhost.com *.localhost.com
```

### Docker

```bash
docker-compose up
```

### Compile it yourself

```bash
dotnet cake
```
