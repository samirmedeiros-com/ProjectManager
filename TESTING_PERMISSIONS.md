# 🔒 Testing Gestor Permission System

## System Overview

Implemented role-based permission system where:
- **Admin**: Full access to all user management features
- **Gestor**: Can manage users only in their assigned sectors, can only create Gestor/Utilizador roles
- **Utilizador**: No access to user management
- **Owner**: No access to user management

## Test Credentials

### Admin User
- Email: `admin@example.com`
- Password: `admin123`
- Access: Full user management (all setores, all roles)

### Gestor User (needs to be created)
- Email: `gestor@example.com`
- Password: `gestor123`
- Role: Gestor
- Setores: (assign to specific setores)
- Access: Can manage users only in assigned setores

### Regular User
- Email: `ana@example.com`
- Password: `ana123`
- Role: Utilizador
- Access: ❌ Cannot access user management

## Test Cases

### 1. Admin User Tests
As `admin@example.com`:

✅ **Create User**
- Navigate to "👥 Gerir Utilizadores"
- Click "+ Registar Utilizador"
- Create user with ANY role (Owner, Admin, Gestor, Utilizador)
- Assign to ANY setores
- Should succeed

✅ **Edit User**
- Click edit (✏️) on any user
- Change role to any role
- Change setores to any combination
- Should succeed

✅ **Delete User**
- Click delete (🗑️) on any user
- Should succeed

✅ **View All Users**
- Should see ALL users in the system
- Setores dropdown should show ALL available setores

---

### 2. Gestor User Tests

#### Setup (as Admin)
1. Create a test Gestor:
   - Email: `gestor-test@example.com`
   - Password: `test123`
   - Role: Gestor
   - Assign to 2-3 setores (e.g., "TI" and "RH")

#### Tests (as Gestor)

❌ **Cannot See User Management (if not assigned to setores)**
- If Gestor has NO setores, should be redirected to dashboard

✅ **Can See User Management (if assigned to setores)**
- If Gestor has setores, can see "Gerir Utilizadores"

✅ **Create User with Allowed Roles ONLY**
- Role dropdown shows: Gestor, Utilizador ONLY
- Cannot select: Admin, Owner
- If role dropdown is empty = BUG!

❌ **Cannot Create Admin/Owner**
- Try to send API request with Admin role
- Should get 403 Forbidden

✅ **Can Only Assign Own Setores**
- Setores checkboxes show ONLY the setores where Gestor has access
- Trying to assign other setores should be prevented (greyed out or not shown)

✅ **Can Edit Users in Own Setores**
- Click edit (✏️) on users from assigned setores
- Should work
- Can change their roles (only to Gestor/Utilizador)
- Can change their setores (only to own setores)

❌ **Cannot Edit Users in Other Setores**
- Users from other setores should have edit button DISABLED
- Button should show: "Sem permissão neste setor"

❌ **Cannot Delete Users**
- Delete button (🗑️) should be DISABLED for Gestores
- Button tooltip: "Apenas admins podem deletar"

✅ **Filter by Setores**
- User list should show ONLY users assigned to Gestor's setores
- Other users should NOT appear

---

### 3. Regular User Tests (No Access)

As `ana@example.com`:

❌ **Cannot Access User Management Page**
- Try to navigate to http://localhost:4200/users
- Should be redirected to /dashboard

❌ **No Button in Dashboard**
- "👥 Gerir Utilizadores" button should NOT appear
- Only Admin/Gestor see this button

❌ **API Access Blocked**
- Try direct API call: `curl -H "Authorization: Bearer TOKEN" http://localhost:5000/api/users`
- Should get 403 Forbidden

---

## Backend Permission Validation

### JWT Claims
Check that role is included in JWT token:
```bash
# 1. Login as Gestor
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"gestor@example.com","password":"gestor123"}'

# 2. Copy the token from response
# 3. Decode token at jwt.io or decode locally:
# The token should contain: "role": "Gestor"
```

### API Endpoint Tests

**Gestor trying to create Admin user:**
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Authorization: Bearer GESTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName":"Test User",
    "email":"test@example.com",
    "password":"test123",
    "role":"Admin",
    "setorIds":[1]
  }'

# Expected: 403 Forbidden or error message
```

**Gestor trying to assign unallowed setor:**
```bash
curl -X PUT http://localhost:5000/api/users/5 \
  -H "Authorization: Bearer GESTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName":"Updated Name",
    "email":"user@example.com",
    "role":"Utilizador",
    "isActive":true,
    "setorIds":[999]
  }'

# Expected: 403 Forbidden (setor 999 doesn't exist or Gestor has no access)
```

---

## Expected Behavior Summary

| Action | Admin | Gestor | Utilizador |
|--------|-------|--------|-----------|
| See "Gerir Utilizadores" button | ✅ | ✅ (if has setores) | ❌ |
| Access /users page | ✅ | ✅ (if has setores) | ❌ |
| View all users | ✅ | ❌ (only own setor users) | ❌ |
| Create Admin | ✅ | ❌ | ❌ |
| Create Owner | ✅ | ❌ | ❌ |
| Create Gestor | ✅ | ✅ | ❌ |
| Create Utilizador | ✅ | ✅ | ❌ |
| Edit any user | ✅ | ❌ | ❌ |
| Edit own setor users | ✅ | ✅ | ❌ |
| Assign any setor | ✅ | ❌ | ❌ |
| Assign own setores | ✅ | ✅ | ❌ |
| Delete users | ✅ | ❌ | ❌ |

---

## Debugging Tips

1. **Check Browser DevTools → Network**
   - Look for 403 responses (forbidden)
   - Check Authorization header is being sent

2. **Check Console Logs**
   - Frontend errors will show in console
   - Check error messages from failed API calls

3. **Check Backend Logs**
   - Look for "Sem acesso" (no access) messages
   - Check UnauthorizedAccessException errors

4. **Common Issues**
   - Token not being sent: Check localStorage for token
   - Wrong role in dropdown: Check getAvailableRoles() method
   - Setores not filtered: Check getAvailableSetores() method
   - Buttons not disabled: Check canEditUser() method
