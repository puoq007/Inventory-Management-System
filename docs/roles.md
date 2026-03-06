# Role & Permissions Hierarchy (Organizer Tree)

This document outlines the roles within the Jig Inventory system and their respective access levels.

## 1. Admin
The highest level of access. Has full control over everything.
- **View:** Dashboard, Current Status, History
- **Manage Data:** 
  - Jig Specs (Create/Edit/Delete/Import)
  - Locations (Create/Edit/Delete/Import)
  - Physical Jigs (Create/Edit/Delete/Import)
- **Administration:**
  - Users (Create/Edit/Delete any role)

## 2. Engineer & Production Lead (ProdLead)
Mid-level technical and operational roles. Can manage Jig data but cannot manage users.
- **View:** Dashboard, Current Status, History
- **Manage Data:**
  - Jig Specs (Create/Edit/Delete/Import)
  - Locations (Create/Edit/Delete/Import)
  - Physical Jigs (Create/Edit/Delete/Import)
- **Administration:**
  - *No access* to User Management.

## 3. Operator
Standard operational role for interacting with jigs on the floor.
- **View:** Dashboard, Current Status, History
- **Manage Data:**
  - Physical Jigs (Create/Edit/Delete/Import)
- **Administration:**
  - *No access* to User Management.
  - *No access* to Jig Specs or Locations.

## 4. Guest
Read-only access across the board, mainly for monitoring status.
- **View:** Dashboard, Current Status
- **Manage Data:**
  - *No access* to History.
  - *No access* to any management pages (Users, Specs, Locations, Physical Jigs).

