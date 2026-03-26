# M365 Manager

A simple, user-friendly Windows desktop application for managing Microsoft 365 users, licenses, and groups. Designed for office managers and secretaries — no IT experience required.

---

## What This App Does

- **Create new employee accounts** in Microsoft 365 with one form
- **Manage existing employees** — reset passwords, enable/disable accounts, update profiles
- **Assign and remove licenses** (Office 365, Microsoft 365, etc.)
- **Manage Teams & Groups** — see who's in which group and add or remove people
- **Dashboard** showing quick stats about your organisation

---

## 🛠️ Tech Stack

| Technology | Purpose |
|------------|---------|
| **.NET 8.0** | Modern cross-platform framework |
| **WinUI 3 (Windows App SDK 1.6)** | Native Windows UI framework |
| **Microsoft Graph SDK** | Microsoft 365 API integration |
| **MSAL.NET** | Secure OAuth 2.0 authentication |
| **CommunityToolkit.Mvvm** | Clean MVVM architecture |
| **Serilog** | Structured logging |
| **Inno Setup** | Professional installer creation |

---

## 🔧 Before You Start: Azure App Registration Setup

Before the app can connect to your Microsoft 365 environment, you need to register it in Azure. This is a one-time setup that takes about 10 minutes.

> **You will need:** A Microsoft 365 account with **Global Administrator** or **Application Administrator** permissions to complete this setup.

### Step 1: Go to Azure Portal

1. Open your web browser and go to **https://portal.azure.com**
2. Sign in with your Microsoft 365 admin account

### Step 2: Register a New Application

1. In the search bar at the top, type **"App registrations"** and click it
2. Click the **"+ New registration"** button
3. Fill in the form:
   - **Name:** `M365 Manager`
   - **Supported account types:** Select **"Accounts in this organizational directory only"** (the first option)
   - **Redirect URI:**
     - Choose **"Public client/native (mobile & desktop)"** from the dropdown
     - Enter: `http://localhost`
4. Click **"Register"**

### Step 3: Copy Your IDs

After registering, you'll see a page with your app's details. You need two values:

1. **Application (client) ID** — Copy this. It looks like: `12345678-abcd-1234-abcd-123456789abc`
2. **Directory (tenant) ID** — Copy this too. Same format.

> **Keep these safe!** You'll enter them into the app's Settings page.

### Step 4: Set Up API Permissions

1. In the left menu, click **"API permissions"**
2. Click **"+ Add a permission"**
3. Choose **"Microsoft Graph"**
4. Choose **"Delegated permissions"**
5. Search for and tick **each** of these permissions:
   - `User.ReadWrite.All`
   - `Directory.ReadWrite.All`
   - `Group.ReadWrite.All`
   - `UserAuthenticationMethod.ReadWrite.All`
6. Click **"Add permissions"**
7. **Important:** Click the **"Grant admin consent for [your organisation]"** button at the top of the permissions list
8. Click **"Yes"** to confirm

> The permissions list should now show green ticks next to each one.

### Step 5: Allow Public Client Flows

1. In the left menu, click **"Authentication"**
2. Scroll down to **"Advanced settings"**
3. Set **"Allow public client flows"** to **Yes**
4. Click **"Save"** at the top

### Step 6: Enter Your IDs in the App

1. Open **M365 Manager**
2. Click **"Open Settings to configure the app"** on the login screen
3. Paste your **Client ID** and **Tenant ID** into the fields
4. Leave the Redirect URI as `http://localhost`
5. Click **"Save Settings"**
6. Click **"Test Connection"** to verify everything works
7. Navigate back and sign in!

---

## System Requirements

- Windows 10 version 1809 or later
- Windows 11 (any version)
- .NET 8.0 Desktop Runtime
- Windows App SDK Runtime 1.5+
- Internet connection

---

## Building from Source

### Prerequisites

- Visual Studio 2022 (17.8+) with these workloads:
  - **.NET Desktop Development**
  - **Windows App SDK C# Templates** (install via VS Installer or as a VSIX)
- .NET 8.0 SDK

### Build Steps

1. Clone or download this repository
2. Open `M365Manager.csproj` in Visual Studio 2022
3. Restore NuGet packages (automatic on first build)
4. Set the platform to **x64** (or x86/ARM64 as needed)
5. Press **F5** to build and run

### NuGet Packages Used

| Package | Purpose |
|---------|---------|
| Microsoft.WindowsAppSDK | WinUI 3 framework |
| Microsoft.Identity.Client | MSAL authentication |
| Microsoft.Graph | Microsoft 365 API calls |
| CommunityToolkit.Mvvm | MVVM architecture |
| CommunityToolkit.WinUI | UI helpers |
| Serilog + Serilog.Sinks.File | Error logging |
| Microsoft.Extensions.Logging | Logging abstraction |
| Microsoft.Extensions.Configuration.Json | Settings file reader |

---

## 📁 Project Structure

```
M365Manager/
├── App.xaml / App.xaml.cs           — App startup, DI container
├── appsettings.json                 — Client/Tenant ID config
├── Helpers/
│   ├── AppSettings.cs               — Settings read/write
│   ├── DialogHelper.cs              — Confirmation dialogs
│   ├── NotificationHelper.cs        — Toast notifications
│   └── PasswordGenerator.cs         — Random password generator
├── Models/
│   ├── AppUser.cs                   — User data model
│   ├── GroupItem.cs                 — Group/Team + Role models
│   └── License.cs                   — License SKU model
├── Services/
│   ├── AuthService.cs               — MSAL authentication
│   ├── GraphUserService.cs          — User CRUD via Graph API
│   ├── GraphGroupService.cs         — Group/Team management
│   └── LicenseService.cs            — License assignment
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── CreateUserViewModel.cs
│   ├── ManageUserViewModel.cs
│   ├── GroupsViewModel.cs
│   └── SettingsViewModel.cs
└── Views/
    ├── MainWindow.xaml              — App shell with NavigationView
    ├── Converters.cs                — Shared value converters
    ├── DashboardPage.xaml
    ├── CreateUserPage.xaml
    ├── ManageUserPage.xaml
    ├── GroupsPage.xaml
    └── SettingsPage.xaml
```

---

## Security Notes

- **Tokens are never saved to disk.** MSAL's in-memory token cache is used; tokens only live in memory while the app is running.
- **No Global Admin role assignment.** The app intentionally does not allow assigning the Global Admin role to prevent accidental privilege escalation.
- **All actions require confirmation.** A dialog always asks "Are you sure?" before making changes.
- **Errors are logged locally** to a `logs/` folder for troubleshooting. Logs do not contain passwords or tokens.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Your account lacks admin permissions" | Sign in with an account that has User Administrator or Global Administrator role |
| "Could not load user statistics" | Check your internet connection and verify API permissions have admin consent |
| Sign-in window doesn't appear | Make sure "Allow public client flows" is enabled in Azure (Step 5 above) |
| License assignment fails with "UsageLocation" | The user needs a usage location set — update their profile first |
| App won't start | Install the .NET 8.0 Desktop Runtime and Windows App SDK Runtime |

---

## 📜 Version History

- **1.0.0** — Initial release with user creation, management, groups, and license assignment

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👤 Author

**1ronman101** — Software Developer

I'm a software developer with expertise in:
- 🖥️ **Desktop Applications** — WinUI 3, WPF, Windows Forms
- ☁️ **Microsoft 365 & Azure Integration** — Graph API, Azure AD, MSAL
- 🌐 **Web Development** — ASP.NET Core, Blazor, React
- 📊 **Business Automation** — Custom tools to streamline workflows

### 💼 Available for Hire

I'm open to freelance projects and contract work. If you need:
- Custom business software
- Microsoft 365/Azure integrations
- Desktop or web application development
- Automation tools

💻 **GitHub:** [github.com/1ronman101](https://github.com/1ronman101)

---

## ⭐ Support

If you find this project useful, please consider:
- ⭐ Starring the repository
- 🐛 Reporting bugs or suggesting features via [Issues](../../issues)
- 🤝 Contributing via [Pull Requests](../../pulls)
