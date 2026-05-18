-- Migrate existing Topic.Subtopic values into SubTopics and link Stories
SET NOCOUNT ON;

SET NOCOUNT ON;

-- Insert distinct subtopic strings per topic into SubTopics
IF OBJECT_ID('dbo.SubTopics', 'U') IS NOT NULL AND OBJECT_ID('dbo.Topics', 'U') IS NOT NULL
BEGIN
  INSERT INTO dbo.SubTopics (TopicId, Name, Notes, CreatedAt)
  SELECT t.TopicId, LTRIM(RTRIM(t.Subtopic)) AS Name, NULL AS Notes, COALESCE(t.CreatedAt, SYSUTCDATETIME())
  FROM dbo.Topics t
  WHERE t.Subtopic IS NOT NULL AND LTRIM(RTRIM(t.Subtopic)) <> ''
    AND NOT EXISTS (
      SELECT 1 FROM dbo.SubTopics st WHERE st.TopicId = t.TopicId AND st.Name = LTRIM(RTRIM(t.Subtopic))
    );
END

-- For stories that reference a topic with a subtopic string, set Story.SubTopicId to the corresponding SubTopic
IF OBJECT_ID('dbo.Stories', 'U') IS NOT NULL AND OBJECT_ID('dbo.SubTopics', 'U') IS NOT NULL AND OBJECT_ID('dbo.Topics', 'U') IS NOT NULL
BEGIN
  UPDATE s
  SET s.SubTopicId = sub.SubTopicId
  FROM dbo.Stories s
  INNER JOIN dbo.Topics t ON s.TopicId = t.TopicId
  INNER JOIN dbo.SubTopics sub ON sub.TopicId = t.TopicId AND sub.Name = LTRIM(RTRIM(t.Subtopic))
  WHERE s.SubTopicId IS NULL AND t.Subtopic IS NOT NULL AND LTRIM(RTRIM(t.Subtopic)) <> '';
END
