### **Business Requirements: Internal Email Signature Manager**

**Project Goal:** To provide a centralized tool for **The Bays Healthcare Group** to audit, manage, and update employee email signatures via Microsoft Entra ID, including a rotating banner system with historical roll-back capabilities.

---

#### **1. Functional Requirements**

* **Entra ID Audit:** The system must connect to the hospital's Entra ID tenant to compare current user profile data (Name, Title, Dept, Phone) against a defined signature template.
* **Discrepancy Reporting:** Identify and list users whose active signatures are missing, outdated, or inconsistent with their official record.
* **Signature Customization:**
* **Live Preview:** Provide a "Signature Creator" interface to preview the final signature layout before deployment.
* **Banner Management:** Allow uploading of images to be used as promotional banners within the signature.
* **Link Assignment:** Allow a specific hyperlink to be assigned to the active banner image at runtime.


* **Historical Versioning:** Maintain a library of previously uploaded banner images to allow for rapid roll-back to prior campaigns.
* **Targeted Deployment:** Support the automatic update of signatures for specific users or defined security groups.

#### **2. Technical & Security Constraints**

* **User Base:** Optimized for low-concurrency (approx. 2 internal admins).
* **Authentication:** Must support Microsoft Entra ID Single Sign-On (SSO) and Multi-Factor Authentication (2FA) for administrative access.
* **Data Residency:** * Application and images must reside within an **Azure App Service** instance.
* Image persistence must be handled via **App Service Persistent Storage** to survive deployment cycles without external blob storage.


* **Audit Logic:** Must interface with the Microsoft Graph API to pull and push user profile/signature data.

#### **3. User Interface (UI) Requirements**

* **Simplicity:** A single-page application (SPA) feel using **Blazor Server** with **Global Interactivity**.
* **Modules:**
1. **Dashboard:** Audit results and mismatch alerts.
2. **Signature Lab:** Preview, banner selection, and hyperlink assignment.
3. **Upload Gallery:** Historical archive of images with an "Upload New" function.



---
