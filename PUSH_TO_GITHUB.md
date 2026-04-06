# How to Push Archeforge-Unity to GitHub

This guide will help you create a new GitHub repository and push the Archeforge Unity project.

## Prerequisites

1. **GitHub Account**: Create one at https://github.com
2. **Git Installed**: Download from https://git-scm.com/downloads
3. **GitHub CLI (Optional but recommended)**: https://cli.github.com/

## Option 1: Using GitHub CLI (Recommended)

### Step 1: Authenticate with GitHub
```powershell
gh auth login
# Follow the prompts to authenticate
```

### Step 2: Create Repository
```powershell
cd c:\workspace\archeforge-unity
gh repo create archeforge-unity --source=. --public --remote=origin --push
```

This command will:
- Create a new public repository named `archeforge-unity`
- Set it as the origin remote
- Initialize the git repo locally
- Push all files to GitHub

### Step 3: Done!
Your repository is now live at `https://github.com/YOUR-USERNAME/archeforge-unity`

---

## Option 2: Using Git Commands Only

### Step 1: Initialize Local Repository
```powershell
cd c:\workspace\archeforge-unity
git init
git add .
git commit -m "Initial commit: Archeforge Unity C# port"
```

### Step 2: Create Repository on GitHub

1. Go to https://github.com/new
2. Enter repository name: `archeforge-unity`
3. Choose visibility: **Public** (or Private if preferred)
4. Do NOT initialize with README, .gitignore, or license (we already have them)
5. Click **Create repository**

### Step 3: Connect to Remote and Push
```powershell
git remote add origin https://github.com/YOUR-USERNAME/archeforge-unity.git
git branch -M main
git push -u origin main
```

Replace `YOUR-USERNAME` with your actual GitHub username.

### Step 4: Verify
Visit `https://github.com/YOUR-USERNAME/archeforge-unity` to see your repository!

---

## Option 3: Using GitHub Desktop (GUI)

1. Open GitHub Desktop
2. Click **File** → **Add Local Repository**
3. Browse to `c:\workspace\archeforge-unity`
4. Click **Add Repository**
5. Click **Publish repository**
6. Give it the name `archeforge-unity`
7. Choose **Public**
8. Click **Publish Repository**

---

## After Pushing: Next Steps

### 1. Add Collaborators (Optional)
```powershell
gh repo collaborator add USERNAME --permission=push
```

### 2. Set Up Branch Protection (Optional)
```powershell
gh api -X POST repos/YOUR-USERNAME/archeforge-unity/branches/main/protection \
  -f required_status_checks='{contexts:["continuous-integration"]}' \
  -f enforce_admins=true
```

### 3. Create GitHub Pages Documentation (Optional)
```powershell
gh repo edit archeforge-unity --enable-pages
```

### 4. Clone on Another Machine
```powershell
git clone https://github.com/YOUR-USERNAME/archeforge-unity.git
cd archeforge-unity
```

---

## Making Updates

Once your repository is on GitHub, make changes locally and push:

```powershell
git add .
git commit -m "Describe your changes here"
git push origin main
```

## Branching for Features

Create a feature branch for new work:

```powershell
git checkout -b feature/new-feature
# Make your changes
git add .
git commit -m "Add new feature"
git push origin feature/new-feature

# Create a Pull Request on GitHub to merge back to main
```

---

## Troubleshooting

### Authentication Issues
If you get an authentication error:
```powershell
# Log out and back in
gh auth logout
gh auth login
```

Or set up SSH:
```powershell
gh ssh-key create
# Follow prompts to add SSH key to GitHub
```

### Permission Denied
Make sure you're using the correct remote URL and that your account has permission.

### Large File Issues
If you have large files in the repo, consider using Git LFS:
```powershell
git lfs install
git lfs track "*.unitypackage"
git add .gitattributes
git commit -m "Configure Git LFS"
```

### Accidentally Pushed Sensitive Data
Use GitHub CLI to remove from history:
```powershell
gh secret set SENSITIVE_DATA --body ""
```

---

## Resources

- GitHub Documentation: https://docs.github.com
- Git Guide: https://git-scm.com/doc
- GitHub CLI Reference: https://cli.github.com/manual

---

## Support

If you encounter issues:
1. Check GitHub Status: https://www.githubstatus.com
2. Review GitHub Docs: https://docs.github.com
3. Check Git troubleshooting: https://git-scm.com/book/en/v2/Git-Tools-Debugging

Happy coding! 🎮
