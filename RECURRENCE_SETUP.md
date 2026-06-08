# Recurring Events - Setup Instructions

## Overview
A complete recurring events feature has been implemented in the Project Manager application. Users can create events that repeat daily, weekly, monthly, or yearly with customizable end conditions.

## Database Schema

The following columns need to be added to the `EVENTS` table in Oracle:

```sql
ALTER TABLE EVENTS ADD ISRECURRENCEPARENT NUMBER(1) DEFAULT 0;
ALTER TABLE EVENTS ADD PARENTEVENTID NUMBER(10);
ALTER TABLE EVENTS ADD RECURRENCEDAYSOFWEEK VARCHAR2(100);
ALTER TABLE EVENTS ADD RECURRENCEENDCOUNT NUMBER(10);
ALTER TABLE EVENTS ADD RECURRENCEENDDATE TIMESTAMP(7);
ALTER TABLE EVENTS ADD RECURRENCETYPE VARCHAR2(50);
```

### Automatic Migration
If the manual SQL commands fail with ORA-00955 (object already exists), execute:

```bash
cd ProjectManagerWebAPI
dotnet ef database update
```

If migration fails, see the manual SQL script at: `ProjectManagerWebAPI/add_recurrence_fields.sql`

## Backend Implementation

### Models (Event.cs)
Added fields:
- `ParentEventId` (int?) - Reference to parent event if this is a generated instance
- `RecurrenceType` (string?) - Type: "None", "Daily", "Weekly", "Monthly", "Yearly"
- `RecurrenceDaysOfWeek` (string?) - Comma-separated days for weekly recurrence
- `RecurrenceEndDate` (DateTime?) - When recurrence should end
- `RecurrenceEndCount` (int?) - Number of occurrences limit
- `IsRecurrenceParent` (bool) - True if this is the original recurring event

### Services (EventService.cs)
- `GenerateRecurringEventsAsync()` - Creates recurring event instances based on configuration
- `DeleteRecurrenceSeriesAsync()` - Deletes entire recurring series or single instance
- Updated `CreateEventAsync()` - Automatically generates recurring instances

### API Endpoints
- `POST /api/events` - Create event (supports recurrence parameters)
  - `recurrenceType`: "None" | "Daily" | "Weekly" | "Monthly" | "Yearly"
  - `recurrenceDaysOfWeek`: "Mon,Wed,Fri" (for weekly)
  - `recurrenceEndDate`: ISO date string (optional)
  - `recurrenceEndCount`: number (optional)
  
- `DELETE /api/events/{id}?deleteAll=true` - Delete entire series
- `DELETE /api/events/{id}` - Delete single event

## Frontend Implementation

### Components (agenda.component.ts)
- Form fields for recurrence configuration:
  - Recurrence type selector
  - Weekly day picker (Monday-Sunday)
  - End condition selector (no end/specific date/count)
  - Optional date and count inputs
  
- `toggleWeekday(day)` - Toggle day selection for weekly recurrence
- `deleteRecurrenceEventOnly()` - Delete single event from series
- `deleteRecurrenceSeries()` - Delete entire recurring series
- Recurrence deletion modal - Shows options when deleting recurring events

### UI Features
- Recurrence type dropdown in event creation modal
- Day picker for weekly recurrence (shows only when weekly selected)
- End condition selector with conditional date/count fields
- Modal dialog for deletion confirmation (single event vs. entire series)

### Models Updated
- `Event` - Added recurrence fields for display
- `CreateEventRequest` - Added recurrence configuration fields
- `UpdateEventRequest` - Added recurrence configuration fields

## Usage Examples

### Create a daily recurring event
```javascript
{
  title: "Daily Standup",
  description: "Team standup meeting",
  date: "2026-06-08",
  startTime: "09:00",
  endTime: "09:30",
  projectId: null,
  isApplicableToProject: false,
  recurrenceType: "Daily",
  recurrenceEndCount: 20  // 20 occurrences
}
```

### Create a weekly recurring event (Monday, Wednesday, Friday)
```javascript
{
  title: "Team Meeting",
  description: "Weekly sync",
  date: "2026-06-09",  // A Monday
  startTime: "14:00",
  endTime: "15:00",
  projectId: null,
  isApplicableToProject: false,
  recurrenceType: "Weekly",
  recurrenceDaysOfWeek: "Mon,Wed,Fri",
  recurrenceEndDate: "2026-12-31"  // End on Dec 31
}
```

### Create a monthly recurring event
```javascript
{
  title: "Monthly Report",
  date: "2026-06-01",
  startTime: "10:00",
  endTime: "11:00",
  recurrenceType: "Monthly",
  recurrenceEndCount: 12  // 12 months
}
```

## Testing

### Test Case 1: Create Daily Event
1. Navigate to Agenda
2. Select a date
3. Click "+ Novo Evento"
4. Enter title, time
5. Select Recurrence: "Diária"
6. Select end type: "Após N ocorrências"
7. Enter count: 5
8. Click "Criar Evento"
9. Verify 5 events appear on consecutive days

### Test Case 2: Create Weekly Event
1. Create event with Recurrence: "Semanal"
2. Select days: Segunda, Quarta, Sexta
3. Set end date 1 month from start
4. Verify events appear on M/W/F only

### Test Case 3: Delete Single Recurring Event
1. Click delete button on any recurring event instance
2. Modal appears asking "Deletar Apenas Este" or "Deletar Toda a Série"
3. Click "Deletar Apenas Este"
4. Verify only that event is removed, others remain

### Test Case 4: Delete Entire Series
1. Click delete button on any recurring event instance
2. Modal appears
3. Click "Deletar Toda a Série"
4. Verify all events in the series are removed

## Architecture

```
User creates recurring event
        ↓
EventService.CreateEventAsync()
        ↓
Event saved to DB with IsRecurrenceParent = true
        ↓
GenerateRecurringEventsAsync() called
        ↓
Loop through dates based on RecurrenceType:
  - Daily: Each day until endDate/endCount
  - Weekly: Only on selected days (RecurrenceDaysOfWeek)
  - Monthly: Same day each month
  - Yearly: Same day each year
        ↓
Each instance created with:
  - ParentEventId = original event ID
  - IsRecurrenceParent = false
  - Same title, description, time, project
        ↓
All instances saved to DB
        ↓
Frontend displays all events (parent + instances)
```

## Known Limitations

1. **Database Migration**: Oracle compatibility issue (ORA-00955) - Manual SQL execution may be required
2. **Edit Recurring Events**: Currently not implemented - would require asking user if they want to edit just that instance or the entire series
3. **Weekly Recurrence**: Generates events every day and filters by day - could be optimized
4. **Timezone**: Uses UTC for all datetime operations

## Future Enhancements

- [ ] Edit recurring events with series/instance options
- [ ] Recurrence rules display on event card (e.g., "Daily until Dec 31")
- [ ] Conflict detection for recurring events
- [ ] Recurring event templates/presets
- [ ] Calendar view of recurring series
- [ ] Recurrence pattern customization (e.g., "every 2 weeks")
- [ ] Holiday exclusions for recurring events

## Troubleshooting

### Migration won't apply
Check if columns already exist:
```sql
SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'EVENTS';
```

If columns exist, manually insert migration history:
```sql
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
VALUES ('20260608064036_AddRecurrenceColumnsManually', '10.0.8');
COMMIT;
```

### Recurring events not generating
1. Verify RecurrenceType is not "None"
2. Check if RecurrenceEndDate is in the future (or null)
3. Check RecurrenceEndCount > 0
4. Review backend logs for GenerateRecurringEventsAsync errors

### Deletion not working
- Verify deleteAll parameter is being passed correctly
- Check if user owns the event (UserId match)
- Verify event exists in database

---

**Implementation Date**: 2026-06-08  
**Status**: ✅ Complete (Database schema manual application required)
