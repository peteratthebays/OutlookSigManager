var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.OutlookSigManager>("outlooksigmanager");

builder.Build().Run();
