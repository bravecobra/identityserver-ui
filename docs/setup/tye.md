# Setup through Project Tye

[Project tye](https://github.com/dotnet/tye) offers a lightweight orchestrator that allows you to run your projects locally easily. The underlying engine is still `Docker` though, but it builds your projects on the fly for you.

Make sure you have your .NET developer certfificate trusted as tye autmatically uses them. For local projects, it basically runs a proxy inside a container with a volume mount that points back to your source folder.

```powershell
dotnet dev-certs https --trust
```

## Install Tye

First you need to install tye on your machine. Just follow the [getting started guide](https://github.com/dotnet/tye/blob/master/docs/getting_started.md). It just installs as a dotnet tool.

## Run the project

Everything should be preconfigured.

```powershell
tye run
```

Once started you can access the [Tye dashboard](https://localhost:8000).

The SQL Server instance should be accessible through localhost on port 1433 as well.

The application is running on.

* [STS](https://localhost:5000)
* [JSCLient](https://localhost:5003)
* [MVCClient](https://localhost:5002)

You can also debug these, by attaching to the processes in VS. Have look at the [commandline arguments of tye run](https://github.com/dotnet/tye/blob/master/docs/reference/commandline/tye-run.md), allowing some unique approaches.

## Troubleshooting

Since Linux is a bit more strict on certificates, running this on linux might not just work out of the box if you don't have your dev-certs up and running yet. There is a [workaround](https://github.com/dotnet/tye/blob/master/docs/tutorials/hello-tye/00_run_locally.md#certificate-is-invalid-exception-on-linux) for this however.
