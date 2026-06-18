BEGIN TRAN;

UPDATE dbo.AspNetUsers
SET FullName = N'Kent Monsen'
WHERE Id = 'dd58d1e6-bd51-46b8-8abd-42b0522314a2'
  AND FullName = N'kentmonsen@gmail.con';

IF @@ROWCOUNT <> 1
BEGIN
ROLLBACK;
THROW 50001, 'Forventa å oppdatere nøyaktig éin brukar med FullName = kentmonsen@gmail.con.', 1;
END;

SELECT Id, UserName, Email, FullName
FROM dbo.AspNetUsers
WHERE Id = 'dd58d1e6-bd51-46b8-8abd-42b0522314a2';

COMMIT;