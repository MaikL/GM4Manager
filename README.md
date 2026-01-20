# GM4Manager
### **NTFS & Active Directory Permission Manager (Open Source)**
A practical Windows tool that allows managers (or delegated groups) to
**independently create and manage folders and permissions** — without involving IT every time.

---

## 💡 Motivation

Have you ever been in this situation?

- “Please create a new shared folder for Fritz, George, and Lisa.”
- A week later: “Lisa now only needs **read access**.”
- And George moved to another department — remove his access.

That’s why **GM4Manager** exists.

It allows IT departments to delegate folder permission management to
**department managers or dedicated manager groups**,
using Active Directory’s delegation model — without granting excessive rights or exposing the entire AD.

GM4Manager solves a known Active Directory limitation:

> AD normally allows **only one** “Managed By” owner.
> GM4Manager works around this by managing the **group’s (member)[https://learn.microsoft.com/en-us/windows/win32/adschema/a-member] attribute**,
> enabling **multiple managers** or **manager groups**.

---

## 🧭 How GM4Manager Works

1. Create a security group, e.g.
   **CS_02_Commercial_RW**

2. Create a corresponding manager group, e.g.
   **CS_02_Commercial_Manager**

3. Assign the manager group as **Manager (Managed By)** of the RW group.

4. Users in the **manager** group can now:
   - Add/remove users & groups
   - Change permissions (Readonly ↔ Modify)
   - Create new subfolders
   - Manage NTFS ACLs
   - Manage share permissions

---

## 🖼 Screenshots

### Managed By
![Managed By](/Screenshots/ManagedBy.jpg)

### Active Directory Search
![Search](/Screenshots/AddUserFromAD.jpg)

### Successfully Added
![Success](/Screenshots/AddingSuccessful.jpg)

### Explorer View
![Explorer](/Screenshots/Explorer.jpg)

### Change Readonly ↔ Modify
![Readonly to Modify](/Screenshots/Readonly_to_Modify.png)

---

## ✨ Features

### 🔐 Active Directory Management
- Add or remove users and groups
- Support for **multiple managers**
- Delegation via the `member` attribute
- Integrated AD search window

### 📁 NTFS & Share Permissions
- Explorer-style folder navigation
- Toggle Read ↔ Modify instantly
- Display and manage share permissions
- Nested group support
- Display inherited and explicit access rules

### ⚡ Performance & Usability
- Asynchronous operations (no UI freeze)
- Splash screen during startup
- Modern UI (ModernWpf)
- Clear error handling
- Optimized LDAP queries

---

## 🛠 Prerequisites

- Windows (AD environment)
- **.NET 8 Runtime**
- Optional: run as another user
  https://github.com/MaikL/RunProgramAs

---

## 📥 Installation

Download the installer:

👉 **[GM4ManagerSetup.exe](Output/GM4ManagerSetup.exe)**

Installer built with **Inno Setup**.

---

## 🚀 Usage

1. Start GM4Manager
2. Navigate to a group or folder
3. Add/remove users or groups
4. Adjust NTFS permissions (Read ↔ Modify)
5. Create new directories
6. Review share permissions

---

## 🧱 Project Structure

- `App.xaml` — Main application entry
- `ManagerUC.xaml` — AD group and member management
- `ExplorerUC.xaml` — NTFS/share permission explorer
- `Helpers/` — LDAP, AD, NTFS utilities
- `Resources/` — Images, icons, assets
- `Installer/` — Inno Setup configuration

---

## ⚠ Known Issues

- Startup may be slow if the user has many AD group memberships
- VPN or slow DC connections can delay LDAP queries
- Share permissions may override NTFS permissions
- Large AD environments may produce slower searches

---

## 🔧 Troubleshooting

- **Slow startup** → Check AD connectivity, reduce excessive group memberships
- **Permission issues** → Verify share permissions AND NTFS permissions
- **Folder creation issues** → Check inheritance settings and share-level rights

---

## 🤝 Contributing

Contributions are welcome!
Feel free to:

- open issues
- submit pull requests
- suggest or request new features

---

## 📄 License

Licensed under the **GPL 3.0 License**
See: LICENSE.md

---

## 🎥 Demo

![GM4Manager Demo](/Screenshots/GM4Manager.gif)

If you find GM4Manager helpful, consider:

- ⭐ starring the repository
- 🗣 recommending it to colleagues
- 🐞 reporting bugs or feature ideas

---

## 🎉 Thank You for Using GM4Manager!