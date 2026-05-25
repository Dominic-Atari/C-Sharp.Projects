# Local dev wiring — head-teacher Create Subject flow

End-to-end sequence from a fresh head-teacher registration through Create Subject, showing how the dev proxy splits traffic between the two Functions hosts (`Clients` on :7071 and `Functions` on :7072).

```mermaid
sequenceDiagram
    autonumber
    participant Br as Browser
    participant Ng as Angular dev proxy
    participant Cl as Clients host :7071
    participant Fn as Functions host :7072
    participant Auth as AuthorizeHeadTeacher
    participant DB as SQL Server

    Note over Br: Page loads at /auth; user submits register-head form

    Br->>Ng: POST /api/auth/register-head { username, password, schoolName, ... }
    Note over Ng: proxy.conf.json: /api/auth/* -> :7071
    Ng->>Cl: forward
    Cl->>DB: insert User + School + Role(HeadTeacher) + Password
    Cl-->>Br: 201 { token, schoolId, roles, redirectUrl: "/dashboard" }
    Br-->>Br: saveAuth(res); navigateByUrl("/dashboard")

    Note over Br: Head teacher opens school popover and submits Create Subject form

    Br->>Ng: POST /api/schools/{schoolId}/subjects { name, stage, description }<br/>Authorization: Bearer JWT
    Note over Ng: proxy.conf.json: /api (catch-all) -> :7072<br/>(NOT /api/auth, NOT /api/V1)
    Ng->>Fn: forward

    Fn->>Auth: AuthorizeHeadTeacher(req)
    Note over Auth: Validate JWT signature, expiry, issuer.<br/>Check role == HeadTeacher.<br/>Check schoolId in route == schoolId claim.
    alt JWT missing / invalid / expired
        Auth-->>Fn: 401
        Fn-->>Br: 401 Unauthorized
    else role or school mismatch
        Auth-->>Fn: 403
        Fn-->>Br: 403 Forbidden
    else authorized
        Auth-->>Fn: { userId, schoolId }
        Fn->>Fn: parse + validate CreateSubjectRequest
        alt body invalid (missing name, bad stage)
            Fn-->>Br: 400 Bad Request
        else valid
            Fn->>DB: INSERT Subjects (schoolId, name, stage, description)
            DB-->>Fn: subjectId
            Fn-->>Br: 201 { data: { subjectId } }
        end
    end
```
