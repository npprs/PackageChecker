# PackageChecker Setup Instructions

## PREREQUISITES

**Required Software**
- LATEST VRCHAT UNITY VERSION
- VCC CREATOR COMPANION

---

## HOW IT WORKS

This tool ensures your users have the correct package versions installed.

**Overview:**
- Import PackageChecker.unitypackage into your project
- Generate a lock file from your project
- Export your avatar/asset WITH the PackageChecker folder
- Users import as normal - the checker validates packages automatically

---

## SETUP FOR CREATORS

### Generate Lock File

In Unity's top menu, go to:
```
Tools > NOPPERS > PackageChecker > Generate Lock File
```

This captures your current package versions and saves them

### Export Your Asset

Export your avatar/asset as a Unity package as you normally would.

**IMPORTANT:** You MUST include the entire PackageChecker folder and the lock file for the checker to work.

---

## WHAT USERS WILL SEE

### If All Packages Are Correct

Nothing happens - success is silent. Users can start working immediately.

### If Packages Are Missing or Wrong Versions

A popup window will appear listing the issues.

Users will be guided with these options:

**Fix Manually Through VCC (RECOMMENDED)**
- Open VRChat Creator Companion
- Install/update the required packages

**Try Experimental "Fix All" Button**
- Attempts to auto-fix packages
- Requires users to have necessary repositories in VCC

**Dismiss Popup Temporarily**
- Closes the popup

**Disable Lock Files Completely**
- Turns off package checking

---

## IMPORTANT NOTES

**Auto-Fix Limitations:**
- Experimental feature - may not work 100% of the time
- Requires users to have the necessary repositories already added to VCC
- Manual fixes through VCC are more reliable

Dismissed popups will reappear when Unity is reopened, ensuring users don't forget to fix package issues.

---

Please reach out if you have any feedback.