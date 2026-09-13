USE RiceMillDB;
GO

-- Seed o12_designatation if empty
IF NOT EXISTS (SELECT 1 FROM o12_designatation WHERE DesignationId = 1)
BEGIN
    SET IDENTITY_INSERT o12_designatation ON;
    INSERT INTO o12_designatation (DesignationId, DesignationName, IsActive) VALUES 
    (1, 'Admin', 1),
    (2, 'Gate Man', 1),
    (3, 'Weighbridge Man', 1),
    (4, 'Supervisor', 1),
    (5, 'Lab Technician', 1),
    (6, 'Meth', 1),
    (7, 'Worker', 1);
    SET IDENTITY_INSERT o12_designatation OFF;
END;
GO

-- Clean orphaned rows if any before applying constraints
DELETE FROM p1_persondesignatation WHERE DesignationId NOT IN (SELECT DesignationId FROM o12_designatation);
DELETE FROM p1_persondesignatation WHERE PersonId NOT IN (SELECT PersonId FROM p02_Person);

DELETE FROM sa05_user WHERE PersonId NOT IN (SELECT PersonId FROM p02_Person);
DELETE FROM sa05_user WHERE o10_postid NOT IN (SELECT PostId FROM o10_post);
DELETE FROM sa05_user WHERE sa10_usertypeid NOT IN (SELECT UserTypeId FROM sa10_usertype);
DELETE FROM sa05_user WHERE OfficeId IS NOT NULL AND OfficeId NOT IN (SELECT OfficeId FROM o05_office);
GO

-- 1. sa05_user Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_sa05_user_p02_Person')
    ALTER TABLE sa05_user ADD CONSTRAINT FK_sa05_user_p02_Person FOREIGN KEY (PersonId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_sa05_user_o10_post')
    ALTER TABLE sa05_user ADD CONSTRAINT FK_sa05_user_o10_post FOREIGN KEY (o10_postid) REFERENCES o10_post(PostId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_sa05_user_sa10_usertype')
    ALTER TABLE sa05_user ADD CONSTRAINT FK_sa05_user_sa10_usertype FOREIGN KEY (sa10_usertypeid) REFERENCES sa10_usertype(UserTypeId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_sa05_user_o05_office')
    ALTER TABLE sa05_user ADD CONSTRAINT FK_sa05_user_o05_office FOREIGN KEY (OfficeId) REFERENCES o05_office(OfficeId);

-- 2. Person & Designation Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_p1_persondesignatation_p02_Person')
    ALTER TABLE p1_persondesignatation ADD CONSTRAINT FK_p1_persondesignatation_p02_Person FOREIGN KEY (PersonId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_p1_persondesignatation_o12_designatation')
    ALTER TABLE p1_persondesignatation ADD CONSTRAINT FK_p1_persondesignatation_o12_designatation FOREIGN KEY (DesignationId) REFERENCES o12_designatation(DesignationId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_o_designatationreletation_parent')
    ALTER TABLE o_designatationreletation ADD CONSTRAINT FK_o_designatationreletation_parent FOREIGN KEY (o12_parentid) REFERENCES o12_designatation(DesignationId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_o_designatationreletation_child')
    ALTER TABLE o_designatationreletation ADD CONSTRAINT FK_o_designatationreletation_child FOREIGN KEY (o12_childid) REFERENCES o12_designatation(DesignationId);

-- 3. Office Location & Person Location Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_o_OfficeLocation_o05_office')
    ALTER TABLE o_OfficeLocation ADD CONSTRAINT FK_o_OfficeLocation_o05_office FOREIGN KEY (OfficeId) REFERENCES o05_office(OfficeId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_p_PersonLocation_p02_Person')
    ALTER TABLE p_PersonLocation ADD CONSTRAINT FK_p_PersonLocation_p02_Person FOREIGN KEY (PersonId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_p_PersonLocation_o_OfficeLocation')
    ALTER TABLE p_PersonLocation ADD CONSTRAINT FK_p_PersonLocation_o_OfficeLocation FOREIGN KEY (LocationId) REFERENCES o_OfficeLocation(LocationId);

-- 4. Item & Vehicle Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_Item_m_ItemCategory')
    ALTER TABLE m_Item ADD CONSTRAINT FK_m_Item_m_ItemCategory FOREIGN KEY (CategoryId) REFERENCES m_ItemCategory(CategoryId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_VehicleMapping_m_Vehicle')
    ALTER TABLE m_VehicleMapping ADD CONSTRAINT FK_m_VehicleMapping_m_Vehicle FOREIGN KEY (VehicleId) REFERENCES m_Vehicle(VehicleId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_VehicleMapping_Party')
    ALTER TABLE m_VehicleMapping ADD CONSTRAINT FK_m_VehicleMapping_Party FOREIGN KEY (PartyId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_VehicleMapping_Driver')
    ALTER TABLE m_VehicleMapping ADD CONSTRAINT FK_m_VehicleMapping_Driver FOREIGN KEY (DriverId) REFERENCES p02_Person(PersonId);

-- 5. Challan & Inward Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_Challan_p02_Person')
    ALTER TABLE m_Challan ADD CONSTRAINT FK_m_Challan_p02_Person FOREIGN KEY (GeneratorPersonId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_Challan_o10_post')
    ALTER TABLE m_Challan ADD CONSTRAINT FK_m_Challan_o10_post FOREIGN KEY (GeneratorPostId) REFERENCES o10_post(PostId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_Challan_m_StatusMaster')
    ALTER TABLE m_Challan ADD CONSTRAINT FK_m_Challan_m_StatusMaster FOREIGN KEY (StatusId) REFERENCES m_StatusMaster(StatusId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_ChallanItem_m_Challan')
    ALTER TABLE m_ChallanItem ADD CONSTRAINT FK_m_ChallanItem_m_Challan FOREIGN KEY (ChallanId) REFERENCES m_Challan(ChallanId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_m_ChallanItem_m_Item')
    ALTER TABLE m_ChallanItem ADD CONSTRAINT FK_m_ChallanItem_m_Item FOREIGN KEY (ItemId) REFERENCES m_Item(ItemId);

-- 6. Gate Entry Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_GateEntry_m_Vehicle')
    ALTER TABLE t_GateEntry ADD CONSTRAINT FK_t_GateEntry_m_Vehicle FOREIGN KEY (VehicleId) REFERENCES m_Vehicle(VehicleId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_GateEntry_Party')
    ALTER TABLE t_GateEntry ADD CONSTRAINT FK_t_GateEntry_Party FOREIGN KEY (PartyId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_GateEntry_Driver')
    ALTER TABLE t_GateEntry ADD CONSTRAINT FK_t_GateEntry_Driver FOREIGN KEY (DriverId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_GateEntry_o05_office')
    ALTER TABLE t_GateEntry ADD CONSTRAINT FK_t_GateEntry_o05_office FOREIGN KEY (TargetOfficeId) REFERENCES o05_office(OfficeId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_GateEntryLocations_o_OfficeLocation')
    ALTER TABLE t_GateEntryLocations ADD CONSTRAINT FK_t_GateEntryLocations_o_OfficeLocation FOREIGN KEY (LocationId) REFERENCES o_OfficeLocation(LocationId);

-- 7. Unload Transaction Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_UnloadTransaction_t_GateEntry')
    ALTER TABLE t_UnloadTransaction ADD CONSTRAINT FK_t_UnloadTransaction_t_GateEntry FOREIGN KEY (RSTNumber) REFERENCES t_GateEntry(RSTNumber);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_UnloadTransaction_Supervisor')
    ALTER TABLE t_UnloadTransaction ADD CONSTRAINT FK_t_UnloadTransaction_Supervisor FOREIGN KEY (SupervisorId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_UnloadTransaction_Meth')
    ALTER TABLE t_UnloadTransaction ADD CONSTRAINT FK_t_UnloadTransaction_Meth FOREIGN KEY (MethId) REFERENCES p02_Person(PersonId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_UnloadTransaction_m_BagType')
    ALTER TABLE t_UnloadTransaction ADD CONSTRAINT FK_t_UnloadTransaction_m_BagType FOREIGN KEY (BagTypeId) REFERENCES m_BagType(BagTypeId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_UnloadLocation_t_UnloadTransaction')
    ALTER TABLE t_UnloadLocation ADD CONSTRAINT FK_t_UnloadLocation_t_UnloadTransaction FOREIGN KEY (UnloadId) REFERENCES t_UnloadTransaction(UnloadId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_UnloadLocation_o_OfficeLocation')
    ALTER TABLE t_UnloadLocation ADD CONSTRAINT FK_t_UnloadLocation_o_OfficeLocation FOREIGN KEY (LocationId) REFERENCES o_OfficeLocation(LocationId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_WorkerAllocation_t_UnloadTransaction')
    ALTER TABLE t_WorkerAllocation ADD CONSTRAINT FK_t_WorkerAllocation_t_UnloadTransaction FOREIGN KEY (UnloadId) REFERENCES t_UnloadTransaction(UnloadId);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_WorkerAllocation_Worker')
    ALTER TABLE t_WorkerAllocation ADD CONSTRAINT FK_t_WorkerAllocation_Worker FOREIGN KEY (WorkerId) REFERENCES p02_Person(PersonId);

-- 8. Lab Check Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_LabQualityCheck_t_GateEntry')
    ALTER TABLE t_LabQualityCheck ADD CONSTRAINT FK_t_LabQualityCheck_t_GateEntry FOREIGN KEY (RSTNumber) REFERENCES t_GateEntry(RSTNumber);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_t_LabQualityCheck_TestedBy')
    ALTER TABLE t_LabQualityCheck ADD CONSTRAINT FK_t_LabQualityCheck_TestedBy FOREIGN KEY (TestedBy) REFERENCES p02_Person(PersonId);
GO
