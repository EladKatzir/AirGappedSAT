SATPREP - QUICK SETUP (air-gapped Windows Server 2022)
=====================================================

1. Copy this whole "release" folder to a USB stick.
   (From GitHub: Code > Download ZIP, then take the "release" folder.)
2. On the server, copy the folder to the desktop (or anywhere).
3. Double-click SETUP.bat and accept the Administrator prompt.
4. Choose how to host it:
     [1] Windows Service  - recommended. Nothing else is installed.
                            Manage it in Server Manager > Tools > Services.
     [2] IIS website      - installs the IIS role (Server Manager > Tools >
                            IIS Manager) and the ASP.NET Core Module from the
                            bundled offline installer, then creates the
                            "SatPrep" site and application pool.
5. When it says "SatPrep is installed", open  http://localhost:5100
   (other PCs: http://SERVER-NAME:5100), type your name, and study.

That's it. The setup verifies every file (SHA256SUMS.txt), so a damaged USB
copy is detected before anything is installed.

Later
-----
Upgrade : copy the new release folder over, run SETUP.bat again
          (progress is kept, the database is backed up first).
Backup  : C:\SatPrep\Backup-SatPrep.ps1   (-Schedule = every night)
Remove  : C:\SatPrep\Uninstall-SatPrep.ps1

Files
-----
SETUP.bat / Setup-SatPrep.ps1   the setup
SatPrep-v*-win-x64.zip          the app (self-contained, no .NET needed)
prereqs\                        Microsoft ASP.NET Core Hosting Bundle (IIS mode only;
                                stored in parts because GitHub limits file size -
                                SETUP joins them automatically)
SHA256SUMS.txt                  checksums for everything
