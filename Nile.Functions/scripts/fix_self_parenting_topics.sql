-- Fix self-parenting and invalid parent references in Topics
-- 1) Clears ParentTopicId when a topic points to itself
-- 2) Clears ParentTopicId when the referenced parent does not exist or is deleted

UPDATE [dbo].[Topics]
SET ParentTopicId = NULL
WHERE ParentTopicId IS NOT NULL
  AND (
    ParentTopicId = TopicId
    OR NOT EXISTS (SELECT 1 FROM [dbo].[Topics] p WHERE p.TopicId = ParentTopicId AND p.IsDeleted = 0)
  );

-- Report rows affected
SELECT 'Fixed count' = @@ROWCOUNT;
