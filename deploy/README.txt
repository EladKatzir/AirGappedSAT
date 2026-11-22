SATPREP - OFFLINE SAT PRACTICE
Air-gapped deployment for Windows Server 2022
=============================================

Same model as ShiftManager: one self-contained folder. No .NET install, no
IIS, no internet. SatPrep runs as a Windows Service that starts with the
server.


INSTALL (first time)  -  about 2 minutes
----------------------------------------
1. Copy SatPrep-vX.Y.Z-win-x64.zip from the USB stick to the server.
2. (Recommended) Check the zip is intact, in PowerShell:
       Get-FileHash .\SatPrep-vX.Y.Z-win-x64.zip -Algorithm SHA256
   and compare with the .sha256 file next to the zip.
3. Right-click the zip > Properties > tick "Unblock" > OK, then Extract All.
4. In the extracted SatPrep folder, double-click INSTALL.bat and accept the
   Administrator prompt.

   The installer:
     - verifies every DLL against DLL_CHECKSUMS.txt and unblocks the files
     - copies SatPrep to C:\SatPrep
     - creates the "SatPrep - SAT practice" Windows Service
       (automatic start, restarts itself if it crashes)
     - opens Windows Firewall for TCP port 5100
     - starts it and shows the address to open

5. Open http://localhost:5100 on the server, or http://SERVER-NAME:5100 from
   another computer on the network. A "SatPrep" shortcut is placed on the
   desktop. Type your name and start studying.

Options (pass them to INSTALL.bat or Install-SatPrep.ps1):
   -Port 8080                use another port
   -InstallDir D:\SatPrep    install somewhere else
   -NoFirewall               only this server can open SatPrep


MANAGE IT
---------
Server Manager > Tools > Services > "SatPrep - SAT practice"
(start / stop / restart). Errors are written to Event Viewer >
Windows Logs > Application (source "SatPrep").


UPGRADE TO A NEW VERSION
------------------------
Extract the new zip, then run (as Administrator) Update-SatPrep.ps1 from the
new folder, or just INSTALL.bat again. The database is backed up first to
C:\SatPrep\backups, all progress and uploaded question packs are kept, and
database changes are applied automatically.


BACK UP PROGRESS
----------------
Everything (learners, answers, mastery, tests) is in C:\SatPrep\satprep.db.
   Backup-SatPrep.ps1                       backup now to C:\SatPrep\backups
   Backup-SatPrep.ps1 -Destination E:\Bak   backup to a USB drive
   Backup-SatPrep.ps1 -Schedule             automatic daily backup at 02:30
or use the "Download backup" button on the Content page in the app.
To restore: stop the service, copy the backup over C:\SatPrep\satprep.db,
start the service.


ADD QUESTIONS
-------------
Content page > "Add a question pack" (JSON, format in
Content\packs\README.md), or copy .json packs into C:\SatPrep\Content\packs
and press "Re-import packs folder". Single questions can be added or fixed
with "New question" / "Edit".


QUICK TEST WITHOUT INSTALLING
-----------------------------
Double-click START_HERE.bat: runs SatPrep in a console window until you
close it (use INSTALL.bat for permanent hosting).


UNINSTALL
---------
Uninstall-SatPrep.ps1             removes the service and firewall rule,
                                  keeps C:\SatPrep (your data)
Uninstall-SatPrep.ps1 -RemoveFiles   also deletes C:\SatPrep (a final copy
                                  of the database is saved to Documents)


TROUBLESHOOTING
---------------
"Could not load file or assembly"  -> files are still blocked; run INSTALL.bat
                                      again (it unblocks everything).
Page does not open from other PCs   -> check the firewall rule
                                      "SatPrep HTTP (TCP 5100)" exists.
Port already in use                 -> INSTALL.bat -Port 5200
