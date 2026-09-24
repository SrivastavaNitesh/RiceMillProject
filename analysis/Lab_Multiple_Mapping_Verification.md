# Multiple lab-test mapping

The mapping form now uses a checkbox multi-select dropdown. Selecting a category filters items; selecting an existing item loads all its saved active tests. Edit links load the whole item group. Selection errors retain submitted values. Unselecting all tests asks for confirmation.

The new sp_LabMappingSetSave procedure validates access, the active category/item pair and active tests. It checks the original selection for concurrent changes and updates the complete test set in one transaction. Deselected mappings are marked inactive; new selections get mapping rows; unchanged mappings retain their IDs. Existing report snapshots are untouched. No foreign keys or hard deletes are introduced. Existing individual Remove controls remain available.

Deployment: Lab_Multiple_Test_Mapping.sql was installed on the configured database without changing any existing mappings. Application changes require rebuilding/restarting the running app.

Verification: build passed with two pre-existing nullable warnings. Eleven HTTP checks and seven browser checks passed against the isolated RiceMillLab_Test_01a0abba database. SQL verified retained inactive history, the expected active mapping set and zero foreign keys. Evidence: lab_multi_mapping_http_results.txt and lab_multi_mapping_ui_results.txt. No real mapping was changed for testing; no commit or push performed.