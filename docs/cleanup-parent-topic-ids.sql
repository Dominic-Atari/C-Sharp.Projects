-- Cleanup invalid ParentTopicId values in Topics table
-- Run in your NileDb (test/dev) after backing up the data

-- 1) Null-out self-referential ParentTopicId
UPDATE dbo.Topics
SET ParentTopicId = NULL
WHERE ParentTopicId IS NOT NULL AND ParentTopicId = TopicId;

-- 2) Null-out references to missing topics
UPDATE t
SET ParentTopicId = NULL
FROM dbo.Topics t
LEFT JOIN dbo.Topics p ON t.ParentTopicId = p.TopicId
WHERE t.ParentTopicId IS NOT NULL AND p.TopicId IS NULL;

-- 3) Null-out parents that belong to a different school or subject
UPDATE t
SET ParentTopicId = NULL
FROM dbo.Topics t
JOIN dbo.Topics p ON t.ParentTopicId = p.TopicId
WHERE t.ParentTopicId IS NOT NULL AND (t.SchoolId <> p.SchoolId OR t.SubjectId <> p.SubjectId);

-- After running, verify the results
SELECT COUNT(*) AS TotalTopics, SUM(CASE WHEN ParentTopicId IS NULL THEN 1 ELSE 0 END) AS ParentNullCount
FROM dbo.Topics;

SELECT TOP 100 TopicId, ParentTopicId, Subtopic, Name FROM dbo.Topics WHERE ParentTopicId IS NOT NULL ORDER BY ParentTopicId;
