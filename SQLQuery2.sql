insert into AspNetUserRoles (UserId, RoleId) 
select u.Id, r.Id
from AspNetUsers u, AspNetRoles r
where u.UserName = 'ana' and r.Name= 'Admin';