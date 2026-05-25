# N.LMS.Database.Interface

Owns the EF Core model for the `NileDB` database.

## ERD (logical)

```
Cohort 1───* User *───* Course (as Instructor)
                  │       │
                  *       *
              Enrollment ─┘
                          │
                       Module 1───* Lesson
```

| Aggregate | Key            | Notes                                                                   |
|-----------|----------------|-------------------------------------------------------------------------|
| Cohort    | `CohortId`     | Optional grouping of Users (e.g. graduating class, employer cohort).    |
| User      | `UserId`       | Unique `Email`. Soft-deleted via `Deleted`. Plays a `UserRole`.         |
| Course    | `CourseId`     | Belongs to one Instructor (`User`). Status: Draft/Published/Archived.   |
| Enrollment| `EnrollmentId` | Unique `(UserId, CourseId)`. Tracks progress + role within the course.  |
| Module    | `ModuleId`     | Ordered children of a Course (`SortOrder`).                             |
| Lesson    | `LessonId`     | Ordered children of a Module. Typed content (Article/Video/Quiz/…).     |

## Conventions

- All aggregates carry `Deleted`, `CreatedUtc`, `ModifiedUtc`.
- Soft delete: queries filter `Deleted == false` unless loading deleted is requested explicitly.
- `NileDbContext` clears the change tracker after every `SaveChanges` so callers cannot accidentally re-track previously loaded entities.
- Schema is owned by DbUp scripts under `../N.LMS.Database.DbUp/Scripts/NNN.*.sql`; the EF model is descriptive, not migration-generating.
