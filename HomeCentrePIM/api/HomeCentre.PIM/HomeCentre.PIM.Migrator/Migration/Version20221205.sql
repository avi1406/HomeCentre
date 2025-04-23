CREATE OR REPLACE FUNCTION add_constraint(t_name text, c_name text, constraint_sql text)
RETURNS void
AS $$
BEGIN
    if not exists (select constraint_name 
                   from information_schema.constraint_column_usage 
                   where table_name = t_name  and constraint_name = c_name) then
        execute constraint_sql;
end if;
END;
$$
LANGUAGE plpgsql;


SELECT add_constraint('appuser', 'appuser_employeeno_uq_idx', 'ALTER TABLE appuser ADD CONSTRAINT appuser_employeeno_uq_idx UNIQUE (employeeno);');
