SATPREP - AIR-GAPPED DEPLOYMENT (Windows Server 2022)
=====================================================

Same model as ShiftManager: one self-contained folder, no installs, no internet.

1. Copy SatPrep-vX.Y.Z-win-x64.zip from USB to C:\SatPrep\
2. Verify:  CertUtil -hashfile SatPrep-vX.Y.Z-win-x64.zip SHA256
            (compare with the .sha256 file next to the zip)
3. Right-click the zip > Properties > Unblock > OK, then extract.
4. Double-click START_HERE.bat
5. Browse to http://localhost:5100  (others: http://<server-name>:5100)
   Open the firewall for TCP 5100 if learners connect from other machines.

DATA
  satprep.db          - all users, progress and questions (back this up)
  Content\packs\*.json - question packs, imported automatically on start-up

UPGRADING
  Stop the app, extract the new zip over the old folder, KEEP satprep.db,
  start again. New/changed question packs are merged automatically.

PORT
  Default 5100 (ShiftManager uses 5000). Change "Urls" in appsettings.json.
