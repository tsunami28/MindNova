# Work Items

Local work-item tracker for MindNova. All epics, stories, spikes, and bugs live here as markdown files with YAML front-matter.

## Directory layout

```
docs/discovery/
  backlog.md          - ordered backlog (priority queue)
  epics/              - epic definitions
  stories/            - stories grouped by epic slug
    <epic-slug>/
      <story-slug>.md
  spikes/             - discovery/spike items (research anchors)
  sprints/            - iteration plans (optional)
```

## Key convention

Each work item carries a sequential key: `key: MN-<number>`. To assign a new key, find the highest existing number and increment by one.

## Front-matter schema

```yaml
---
key: MN-1
type: epic            # epic | story | task | bug | spike
status: backlog       # backlog | refined | in-progress | done
epic: MN-1            # parent epic key (omit for epics/spikes)
points: 5             # story points (omit for epics/spikes)
priority: minor       # minor | high | critical
labels: [MindNova]
relates:
  - key: MN-2
    why: "depends on calendar model"
---
```

## Status flow

`backlog` -> `refined` (groomed, ACs written, sized) -> `in-progress` (in a sprint) -> `done`
