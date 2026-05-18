-- 013.RandomlyAssignSubjectsToSchools.sql
-- Randomly assign existing Subjects with NULL SchoolId to one of the known schools.
PRINT 'Running 013.RandomlyAssignSubjectsToSchools.sql';

-- Count rows to update
DECLARE @toUpdate INT = (SELECT COUNT(*) FROM dbo.Subjects WHERE SchoolId IS NULL);
PRINT 'Subjects with NULL SchoolId: ' + CAST(@toUpdate AS VARCHAR(20));

IF @toUpdate = 0
BEGIN
    PRINT 'No Subject rows with NULL SchoolId — nothing to do.';
    RETURN;
END

-- Provide the list of school IDs you gave me — if any of these do not exist in dbo.Schools,
-- the FK (if present) may fail; ensure these are actual SchoolId values in dbo.Schools.
DECLARE @schools TABLE (SchoolId UNIQUEIDENTIFIER);
INSERT INTO @schools (SchoolId) VALUES
    (N'70cf3ae8-8964-4a41-be57-2b28a4a711fc'),
    (N'5f0961e0-d7ba-4ba4-9321-e518a1ce45cd'),
    (N'6715441c-3e2f-4ced-a112-39ba8bf64beb'),
    (N'609e31cc-24ec-499f-bc46-48d8f64a9c61'),
    (N'b1c4fd44-1925-4816-8a44-4cecce33aef2'),
    (N'b1f69c2c-ea60-4bdf-b761-4e0a2b670c83'),
    (N'66d904d5-4769-420e-84ec-53fdb69ffc9e'),
    (N'f48d7612-6eb8-48c1-a832-549ec602c110'),
    (N'4ab7d7c9-fa4a-4c5d-925d-54c9cd471451'),
    (N'08af7505-c89b-41dd-b63f-561bdb6103ce'),
    (N'b595ec25-b4c0-471c-8a1e-5b71453f8db7'),
    (N'a37ec1d5-6e33-4fed-b682-5fc410ddd9f1'),
    (N'5c4353a8-24fa-42bd-9a64-64506ed78f53'),
    (N'1efa80fb-4265-413c-b367-6992869a627c'),
    (N'79b916cd-a149-4b52-8789-6b0a8b749590'),
    (N'04b133e9-67d1-49a6-bfd3-6cdcbb852d7b'),
    (N'bdf71c0c-87db-4713-8e93-7788e95448f2'),
    (N'2c8020ca-6bb0-44ee-a198-7b84711f3a2a'),
    (N'fc8af8f3-980f-4327-97b9-7f7ca9ea83e0'),
    (N'b3c02d10-addc-4cf9-9c49-85672fff3da4'),
    (N'b483423e-19d6-45bb-b805-8ebc149b2fbd'),
    (N'9b28bf52-8b7c-496b-99d4-9b4c87a3d278'),
    (N'9c12fac8-b0df-459e-a8a1-9c4d132f8b87'),
    (N'9e60084f-647a-4cdb-bc6f-b2165897b320'),
    (N'e1928a1f-3eb6-450f-8978-bc9905a9e35f'),
    (N'2a741218-0942-4932-8e02-c562f4c97c31'),
    (N'8dd4cf3e-0325-45e0-a90c-d0c1fdbf4b1b'),
    (N'd8cfd48e-058a-4205-aaec-fc4751d89e98');

-- Update: for each Subject with NULL SchoolId, pick a random SchoolId from the table variable
UPDATE dbo.Subjects
SET SchoolId = (SELECT TOP (1) SchoolId FROM @schools ORDER BY NEWID())
WHERE SchoolId IS NULL;

PRINT 'Rows updated: ' + CAST(@@ROWCOUNT AS VARCHAR(20));
PRINT '013 script completed.';
