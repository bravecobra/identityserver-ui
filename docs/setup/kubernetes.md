# Setup Kubernetes

## Prepare the cluster

I use on [Docker Desktop](https://www.docker.com/products/docker-desktop) `2.2.0.5` which comes with Kubernetes `1.15.5` on Windows and Mac. Testing has only been done on Windows.

Make you also completes the other [prerequisites](../prerequisites/)

### MetalLB

If you're not on a hosted cluster (like GCE, AKS, EKS or DigitialOcean), you'll want to provide a load-balancing implementation in order to have external-ip assigned automatically. I used MetalLB for this on my 1-node local cluster

```powershell
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.9.3/manifests/namespace.yaml
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.9.3/manifests/metallb.yaml
```

if, for some reason, you get the error that the memberlist secret does not exist, create it manually:

```powershell
kubectl create secret generic -n metallb-system memberlist --from-literal=secretkey="$(openssl rand -base64 128)"
```

### Tiller (2.x)

As we use helm to install the nginx-ingress, we'll need to have it installed on our cluster.

```powershell
helm repo add stable https://kubernetes-charts.storage.googleapis.com/
helm repo update
kubectl apply -f ./k8s/infrastructure/tiller/rbac-config.yaml
kubectl apply -f ./k8s/infrastructure/tiller/tiller.yaml
```

## Build the docker images

We need to make sure we build the latest images. We'll use `docker-compose` to do that easily.

```powershell
docker-compose build
```

## Deploy to the cluster

### Create a new namespace

Run the following command to create a new namespace called `identityserver-ui`

```bash
kubectl apply -f ./k8s/infrastructure/namespace.yaml
```

### SQL Server

#### Change the default password

Create a k8s secret for SQL server by first base64 encoding the actual password in powershell or bash, depending on you OS:

```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("Password_123"))
```

or

```bash
echo -n 'Password_123' | base64
```

> Storing a secret as a base64-encoded string is not secure! This is neither hashing or encryption.

Edit the `sqlserverdb-deployment.yaml` if you want another password or override it with:

```bash
kubectl create secret generic sqlserverdb --namespace=identityserver-ui --from-literal=sa_password=Password_123
```

Now deploy the SQL Server onto your kubernetes cluster in the new namespace

#### Deploy SQLServer

```powershell
kubectl apply -f ./k8s/services/sqlserverdb-deployment.yaml
```

### Create the CA Certificate ConfigMap

First add the CA Root certificate as a ConfigMap, that was created during the [certificates setup](../prerequisites/#certificates).

```powershell
kubectl create configmap ca-pemstore --from-file=./compose/nginx/certs/cacerts.pem --namespace=identityserver-ui
```

That way we can mount the CA Root certificate into our services as a configmap (as adding an extra file)

### Deploy STS

Create a config map from the `SeedData.json` file so we can use in the sts service.

```powershell
kubectl create configmap seeddata --from-file=./compose/sts/SeedData.json --namespace=identityserver-ui
```

Next we want to store the db connection string as a secret as well:

```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("Server=sqlserverdb-svc;Database=IdentityUI;User Id=sa;Password=Password_123;MultipleActiveResultSets=true"))
```

> Storing a secret as a base64-encoded string is not secure. This is neither hashing or encryption.

Grab the output and store that in `sts-deployment.yml` file as a secret called `sts-dbconnection` (already done with the above connectionstring). The secret is referenced in the deployment later.

The google credentials are referenced from yet another secret
Create a file called `google-data.yml` in `./k8s/services` that looks like this:

```yaml
kind: Secret
apiVersion: v1
metadata:
  name: sts-google
  namespace: identityserver-ui
data:
  Google_ClientId: <output from your base64 encoded ClientID. Same technique as the DBConnectionstring>
  Google_ClientSecret: <output from your base64 encoded ClientSecret. Same technique as the DBConnectionstring>
type: Opaque
```

```powershell
kubectl apply -f ./k8s/services/google-data.yml
kubectl apply -f ./k8s/services/sts-deployment.yaml
```

### Deploy the API and clients

```powershell
kubectl apply -f ./k8s/services/api-deployment.yaml
kubectl apply -f ./k8s/services/jsclient-deployment.yaml
kubectl apply -f ./k8s/services/mvcclient-deployment.yaml
```

### Map incoming traffic (Ingress controller)

There are several ingress controllers to choose from according to [kubernetes.io](https://kubernetes.io/docs/concepts/services-networking/ingress-controllers/).
To keep it fairly simple, I choose the plain nginx one. You could choose any other like [Istio](https://istio.io/) or [Traefik](https://github.com/containous/traefik). They are bit more involved configuration wise and offer a bit more options.
The ingress controller will replace the nginx-proxy , we used in `docker-compose`.

#### Certificates mount

First add the certificate for all our hosts we want SSL for. We generated this certificate during the `docker-compose` setup.

```powershell
kubectl create secret tls tls-secret --key ./compose/nginx/certs/localhost.com.key --cert ./compose/nginx/certs/localhost.com.crt --namespace=identityserver-ui
```

#### Internal K8S DNS

in order to make the pods be able to resolve the `localhost.com` when they want to verify the certificate, we need to make sure that the internal DNS service of the cluster is able to resolve the A- and CNAME records to the service `nginx-ingress-controller` that'll be created by helm in the next step. In order to do so we can change the CoreDNS configuration to rewrite incoming DNS queries and resolve them to that service.

```powershell
kubectl replace -n kube-system -f ./k8s/infrastructure/coredns.yml
```

#### Ingress

```powershell
helm install --name nginx-ingress stable/nginx-ingress --namespace=identityserver-ui
kubectl apply -f ./k8s/infrastructure/proxy/nginx-ingress.yaml
```

### Testing it out

Everything should now work as intended. Navigate to your deployment:

* [STS](https://sts.localhost.com)
* [JSCLient](https://jsclient.localhost.com)
* [MVCClient](https://mvcclient.localhost.com)
