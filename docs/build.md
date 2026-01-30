### **Project Documentation Structure**

Given your current solution structure (which includes **Aspire** projects like `AppHost` and `ServiceDefaults`), you should keep your documentation at the **root level** of the repository, outside of the specific code projects.

#### **Recommended Folder Path**

Create a directory named **`docs`** at the same level as your `.sln` and `.slnx` files.

* **Path:** `OutlookSigManager/docs/`
* **Why:** This keeps documentation separate from your deployable code assets. It ensures that when you publish the web app to Azure, your "Business Requirements" and "Build Plan" aren't accidentally bundled into the production web server's files.

---

### **Build Plan: Outlook Signature Manager**

I have broken this down into logical phases based on your requirements.

#### **Phase 1: Foundation & Identity (The "Switch")**

* **Task 1.1:** Add Microsoft Entra ID (SSO) authentication via the "Connected Services" wizard or manual `Program.cs` configuration.
* **Task 1.2:** Configure Azure App Service with **Persistent Storage** enabled for the `/home/site/wwwroot/banners` directory.

#### **Phase 2: The Banner Engine (Upload & History)**

* **Task 2.1:** Create the `Signature Lab` page with a file upload component.
* **Task 2.2:** Implement the local JSON-based metadata store to track image-to-hyperlink mappings.
* **Task 2.3:** Build the "Roll-back" UI to allow selection of historical images from the `banners` folder.

#### **Phase 3: The Audit Logic (Entra/Graph Integration)**

* **Task 3.1:** Register the app in Azure to grant `User.Read.All` and `MailboxSettings.ReadWrite` permissions (Microsoft Graph).
* **Task 3.2:** Build the "DB Audit" dashboard to fetch Entra profile data and compare it against your signature template.

#### **Phase 4: Deployment & Automation**

* **Task 4.1:** Finalize the "Sync" function to push the updated signature and banner to specified users/groups.
* **Task 4.2:** Deploy to Azure Web Services.

---
