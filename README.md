1. Createa new ArcGis Pro Project
2. Add the database arxit_esri_case_02835101.gdb
3. Create 2 database users: semo, semohisto
4. Create 2 sde file: semo.sde, semohisto.sde with the respective users 
5. Copy the tables EVENEMENT and PARAM_TEMPORALITE to semohisto.sde
6. Add any Feature Class to semo.sde then Manage / Register as Versioned
7. In Parameters.config
- set the key BDD_PROD_SDE equals to the semo.sde full file path
- set the key BDD_TEMPORELLE_SDE equals to the semohisto.sde full file path
8. Add a debug breakpoint in Services/HistorizationSercice.cs / line 268
8. Run the debug
9. In ArcGIS Pro / Add-In Click on Show DockPanel 1
10. Fill the formular
- Couche ou table à historiser * : enter the name of the feature class (cf: 6) then click Valider
- Champs Identifiant * : select globalid
- Champs libellé * : select any
- Click OK
11. An Exception will rise with error 999999 contact esri support
