CREATE OR REPLACE FUNCTION public.add_constraint(
	t_name text,
	c_name text,
	constraint_sql text)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$

BEGIN
    if not exists (select constraint_name 
                   from information_schema.constraint_column_usage 
                   where table_name = t_name  and constraint_name = c_name) then
        execute constraint_sql;
end if;
END;
$BODY$;

ALTER FUNCTION public.add_constraint(text, text, text)
    OWNER TO lmdev_admin;