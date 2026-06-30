DECLARE @UserId nvarchar(450) = '599bd29d-bc18-4361-9beb-6d2372894950';
DECLARE @MatchId int = 2536593;
DECLARE @Result nvarchar(max) = 'H';

IF EXISTS (
    SELECT 1
    FROM [dbo].[MatchBettings]
    WHERE [UserId] = @UserId
      AND [MatchId] = @MatchId
)
BEGIN
UPDATE [dbo].[MatchBettings]
SET [Result] = @Result
WHERE [UserId] = @UserId
  AND [MatchId] = @MatchId;
END
ELSE
BEGIN
INSERT INTO [dbo].[MatchBettings] ([MatchId], [Result], [UserId])
VALUES (@MatchId, @Result, @UserId);
END;

SELECT *
FROM [dbo].[MatchBettings]
WHERE [UserId] = @UserId
  AND [MatchId] = @MatchId;