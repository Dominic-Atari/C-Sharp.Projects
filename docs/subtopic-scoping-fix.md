Summary of fixes and steps to validate

🔧 What I changed

- Backend validation (SchoolAdminFunction):
  - CreateTopic now validates that if ParentTopicId is supplied it references an existing topic in the same school and subject, otherwise returns 400.
  - UpdateTopic now validates ParentTopicId as above and rejects setting ParentTopicId equal to the topic's own id.

- Frontend (Teacher feed):
  - Tightened name-based matching heuristics in `teacher.page.ts` so server topics are only attached to local (non-server) stories when names match. This prevents broad text matching from attaching subtopics across unrelated stories/topics.

- Data cleanup script: added `docs/cleanup-parent-topic-ids.sql` with safe SQL statements to null-out self-referential or invalid ParentTopicId rows and to identify remaining parent-child relationships.

✅ Why this should fix the duplication

- Many duplicate subtopics were caused by either bad `ParentTopicId` references in the DB (including self-references) or the frontend attaching topics by string matching (name-based heuristics). The backend validation prevents future bad rows; the frontend change prevents incorrect UI attachments when subtopic text is shared across topics.

How to run the cleanup (recommended on dev/test before prod)

1) Backup your DB.
2) Run `docs/cleanup-parent-topic-ids.sql` against the `NileDb` to null out obviously-invalid ParentTopicId values.
3) Re-run the app and confirm the UI no longer shows duplicated subtopics.

Validation checks

- Call GET /schools/{schoolId}/subjects/{subjectId}/topics and inspect parentTopicId values; none should equal their own TopicId, and parents should share schoolId/subjectId.
- Browse the teacher feed and verify subtopics only appear under the parent topic they belong to.
- Create a new parent + child topic via teacher UI and confirm ParentTopicId is set correctly and no duplicates appear.

If you want, I can:
- Add a unit/integration test(s) for CreateTopic/UpdateTopic validation.
- Add an automated DB migration/backfill (DbUp) to fix historical data.

---

File locations:
- Backend: `BackEnd/Nile.Functions/Functions/SchoolAdminFunction.cs`
- Frontend change: `FrontEnd/src/app/pages/teacher/teacher.page.ts`
- Cleanup SQL: `docs/cleanup-parent-topic-ids.sql`
