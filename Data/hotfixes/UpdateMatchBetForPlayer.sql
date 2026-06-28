DECLARE @UserId nvarchar(450) = '0c27a739-5b93-40c7-8012-e4cab39e641e';
DECLARE @MatchId int = 2536574;
DECLARE @Result nvarchar(max) = 'U';

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