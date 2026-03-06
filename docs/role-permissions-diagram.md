# Role Permissions Breakdown

```mermaid
graph TD
    classDef roles fill:#f8fafc,stroke:#cbd5e1,stroke-width:2px,color:#0f172a,rx:8px,ry:8px 
    classDef admin fill:#eff6ff,stroke:#3b82f6,stroke-width:2px,color:#0f172a,rx:8px,ry:8px

    A[/"<b>Admin</b>"<br/>Full Access"\]:::admin --> B
    A --> C
    A --> D
    
    B(["<b>Engineer & ProdLead</b>"<br/>Management without User Control"]):::roles --> C
    B --> D
    
    C({"<b>Operator</b>"<br/>Basic Floor Operation"}):::roles --> D
    
    D[("<b>Guest</b>"<br/>Read-Only Viewing")]:::roles
    
    %% Access Details
    subgraph Admin Setup
    A_Details["- Users Management<br/>- Dashboard & History<br/>- Edit Jig Specs<br/>- Edit Locations<br/>- Manage Physical Jigs"]:::admin
    end
    
    subgraph Mid-level
    B_Details["- Dashboard & History<br/>- Edit Jig Specs<br/>- Edit Locations<br/>- Manage Physical Jigs<br/><i>(Cannot manage users)</i>"]:::roles
    end
    
    subgraph Operators
    C_Details["- Dashboard & History<br/>- Manage Physical Jigs<br/><i>(Cannot edit Specs/Locs)</i>"]:::roles
    end
    
    subgraph Observers
    D_Details["- Dashboard<br/>- Current Status<br/><i>(No editing rights)</i>"]:::roles
    end
    
    A -.- A_Details
    B -.- B_Details
    C -.- C_Details
    D -.- D_Details

    style Admin Setup fill:transparent,stroke:none
    style Mid-level fill:transparent,stroke:none
    style Operators fill:transparent,stroke:none
    style Observers fill:transparent,stroke:none
```
