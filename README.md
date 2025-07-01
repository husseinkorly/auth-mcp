# Authenticated MCP (Model Context Protocol) Server & Client

This repository contains two .NET projects that demonstrate an authenticated Model Context Protocol (MCP) implementation:

1. **MCP Server** - An authenticated HTTP-based MCP server that requires valid Azure AD tokens for access
2. **MCP Client** - A console application that connects to the MCP server using Azure CLI Managed Identity and integrates with Azure OpenAI

## Architecture Overview

```
┌─────────────────┐    HTTP + Bearer Token    ┌─────────────────┐
│                 │  ────────────────────────►│                 │
│   MCP Client    │                           │   MCP Server    │
│                 │  ◄────────────────────────│                 │
└─────────────────┘    MCP Protocol Response  └─────────────────┘
         │                                              │
         │ Azure CLI                                    │ Microsoft Graph API
         │ Managed Identity                             │ (via token acquisition)
         ▼                                              ▼
┌─────────────────┐                            ┌─────────────────┐
│  Azure OpenAI   │                            │ Microsoft Graph │
│                 │                            │                 │
└─────────────────┘                            └─────────────────┘
```

## Prerequisites

- .NET 9.0 or later
- Azure CLI installed and configured
- Azure subscription with:
  - Azure AD tenant
  - Azure OpenAI service
  - Registered application in Azure AD

## Project Structure

```
auth-mcp/
├── auth-mcp.sln                 # Solution file
├── server/                      # MCP Server project
│   ├── Program.cs              # Server entry point
│   ├── appsettings.json        # Server configuration
│   ├── tools/
│   │   └── GraphTools.cs       # Microsoft Graph MCP tools
│   └── server.csproj
├── client/                      # MCP Client project
│   ├── Program.cs              # Client entry point
│   ├── appsettings.json        # Client configuration
│   └── client.csproj
└── README.md
```

## Configuration

### Server Configuration (`server/appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.Hosting": "Information"
    }
  },
  "AllowedHosts": "*",
  "Services": {
    "GraphApiUrl": "https://graph.microsoft.com/v1.0/"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-app-registration-client-id",
    "ClientSecret": "your-app-registration-client-secret"
  }
}
```

### Client Configuration (`client/appsettings.json`)

```json
{
  "AzureAD": {
    "Scopes": ["api://your-app-registration-client-id/.default"]
  },
  "McpServer": {
    "Url": "http://localhost:3001"
  },
  "AzureOpenAI": {
    "DeploymentName": "your-deployment-name",
    "Endpoint": "https://your-openai-resource.cognitiveservices.azure.com"
  }
}
```

## Setup Instructions

### 1. Azure AD App Registration

1. Go to Azure Portal → Azure Active Directory → App registrations
2. Create a new app registration:
   - **Name**: `MCP Server Auth`
   - **Supported account types**: Single tenant
   - **Redirect URI**: Not required for this scenario
3. Note down the **Client ID** and **Tenant ID**
4. Go to **Certificates & secrets** → Create a new client secret
5. Note down the **Client Secret** value
6. Go to **Expose an API** → Add a scope:
   - **Scope name**: `access_as_user`
   - **Admin consent display name**: `Access MCP Server`
   - **Admin consent description**: `Allow access to MCP server`

### 2. Azure CLI Authentication

Ensure you're logged into Azure CLI with an account that has access to your tenant:

```powershell
az login --tenant your-tenant-id
```

Verify your login:
```powershell
az account show
```

### 3. Configure the Server

1. Update `server/appsettings.json` with your Azure AD configuration:
   - Replace `your-tenant.onmicrosoft.com` with your domain
   - Replace `your-tenant-id` with your tenant ID
   - Replace `your-app-registration-client-id` with your client ID
   - Replace `your-app-registration-client-secret` with your client secret

### 4. Configure the Client

1. Update `client/appsettings.json` with your configuration:
   - Replace `your-app-registration-client-id` in the scopes
   - Replace `your-deployment-name` with your Azure OpenAI deployment name
   - Replace `your-openai-resource` with your Azure OpenAI resource name

## Running the Application

### Step 1: Start the MCP Server

```powershell
cd server
dotnet run
```

The server will start on `http://localhost:3001` and display:
- Health check endpoint: `GET /health`
- MCP endpoints with authentication required

### Step 2: Start the MCP Client

In a new terminal:

```powershell
cd client
dotnet run
```

The client will:
1. Load configuration from `appsettings.json`
2. Authenticate using Azure CLI credentials
3. Connect to the MCP server with the Bearer token
4. List available tools from the server
5. Initialize Semantic Kernel with Azure OpenAI
6. Start an interactive chat session

## Usage

Once the client is running, you can interact with it:

```
Assistant Ready! (Type 'exit' to quit)

You: What's my user profile?
Assistant: [AI will use MCP tools to query Microsoft Graph and return your profile information]

You: Who are my colleagues?
Assistant: [AI will use MCP tools to query Microsoft Graph for organizational information]

You: exit
Goodbye!
```