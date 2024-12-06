This repo contains many projects, the two most important ones that should be set as Visual Studio startup projects when trying the app are:

Server: The Asp.Net Core server project / RestApi. Requires NET8.0
BlazorFrontendNew.Client: The Blazor WebAssembly frontend. Contains the ui for normal users, targets old NET5.0 (migration to newer framework planned)

Repo also contains Admin front (also Blazor) but that's just a mostly empty placeholder for now.

Server uses SQL Server for data storage, as well as user management (separate db). Authentication is done with custom JWT token system.

When running the app fresh from Github, server creates two needed databases and seeds some data for demonstrating how the app works. 
