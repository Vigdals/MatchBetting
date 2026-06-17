BEGIN TRY
BEGIN TRANSACTION;

    DECLARE @UserId nvarchar(450) = 'a302868f-0941-4df4-aa9a-247972aaf7a1';
    DECLARE @Toppscorer nvarchar(max) = N'Harry Kane';
    DECLARE @WinnerTeam nvarchar(max) = N'Frankrike';
    DECLARE @MostCards nvarchar(max) = N'Enzo Fernández';

    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[AspNetUsers]
        WHERE [Id] = @UserId
    )
BEGIN
        THROW 50001, 'Fann ikkje brukaren.', 1;
END;

    IF EXISTS (
        SELECT 1
        FROM [dbo].[SideBettings]
        WHERE [UserId] = @UserId
    )
BEGIN
UPDATE [dbo].[SideBettings]
SET
    [Toppscorer] = @Toppscorer,
    [WinnerTeam] = @WinnerTeam,
    [MostCards] = @MostCards
WHERE [UserId] = @UserId;
END
ELSE
BEGIN
INSERT INTO [dbo].[SideBettings]
([Toppscorer], [MostCards], [WinnerTeam], [UserId])
VALUES
    (@Toppscorer, @MostCards, @WinnerTeam, @UserId);
END;

COMMIT TRANSACTION;

SELECT
    u.[Id],
    u.[FullName],
    u.[Email],
    sb.[Toppscorer],
    sb.[WinnerTeam],
    sb.[MostCards]
FROM [dbo].[AspNetUsers] u
    LEFT JOIN [dbo].[SideBettings] sb
ON sb.[UserId] = u.[Id]
WHERE u.[Id] = @UserId;

END TRY
BEGIN CATCH
IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

SELECT
    ERROR_NUMBER() AS ErrorNumber,
    ERROR_MESSAGE() AS ErrorMessage;
END CATCH;