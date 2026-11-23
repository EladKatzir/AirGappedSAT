SATPREP - APP FOLDER REFERENCE
==============================

Normally you do NOT run anything in this folder yourself: double-click
SETUP.bat in the "release" folder. It verifies, extracts and installs this
app for you (Windows Service or IIS website).

Advanced / manual use of the scripts in this folder (PowerShell as Administrator):

  Install-SatPrep.ps1                      install or upgrade as a Windows Service
  Install-SatPrep.ps1 -Mode IIS -HostingBundle <path to dotnet-hosting-*.exe>
                                           install as an IIS website
      options: -Port 8080  -InstallDir D:\SatPrep  -NoFirewall
  Update-SatPrep.ps1                       upgrade, keeping the current mode and port
  Backup-SatPrep.ps1                       back up C:\SatPrep\satprep.db now
  Backup-SatPrep.ps1 -Destination E:\Bak   back up to a USB drive
  Backup-SatPrep.ps1 -Schedule             automatic nightly backup (02:30)
  Uninstall-SatPrep.ps1                    remove service/site + firewall rule, keep data
  Uninstall-SatPrep.ps1 -RemoveFiles       also delete C:\SatPrep (final DB copy -> Documents)
  START_HERE.bat                           quick run in a console window, no install

MANAGE
  Service mode : Server Manager > Tools > Services > "SatPrep - SAT practice"
  IIS mode     : Server Manager > Tools > Internet Information Services (IIS) Manager > Sites > SatPrep
  Errors       : Event Viewer > Windows Logs > Application

DATA
  C:\SatPrep\satprep.db        all learners, answers, mastery and tests (back this up)
  C:\SatPrep\Content\packs     question packs, imported automatically at start-up
  C:\SatPrep\backups           automatic backups made before every upgrade
  Restore: stop SatPrep, copy a backup over satprep.db, start SatPrep.

TROUBLESHOOTING
  "Could not load file or assembly"  run SETUP.bat again (it unblocks every file)
  Not reachable from other PCs       check firewall rule "SatPrep HTTP (TCP 5100)"
  Port already in use                SETUP.bat -Port 5200
  First start is slow                the first start imports all questions (about a minute)
