select *
from Users
where UserName is null

select *
from Users
where len(UserName) > 50

select UserName, count(*) as Cnt
from Users
group by UserName
having count(*) > 1

alter table Users
alter column UserName nvarchar(50) not null

alter table Users
add constraint UQ_Users_UserName unique (UserName)