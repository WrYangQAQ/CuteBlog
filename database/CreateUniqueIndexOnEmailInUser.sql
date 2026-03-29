select 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    COLLATION_NAME
from INFORMATION_SCHEMA.COLUMNS
where TABLE_NAME = 'Users'
  and COLUMN_NAME = 'Email'

alter table Users
alter column Email nvarchar(50) not null

alter table Users
add constraint UQ_Users_Email unique (Email)
