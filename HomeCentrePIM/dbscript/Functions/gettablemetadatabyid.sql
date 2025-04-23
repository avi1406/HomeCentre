CREATE OR REPLACE FUNCTION public.gettablemetadatabyid(
	_id integer)
    RETURNS TABLE(table_id bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
   RETURN QUERY
    SELECT id FROM tablemetadata WHERE id = _id;
END;
$BODY$;

ALTER FUNCTION public.gettablemetadatabyid(integer)
    OWNER TO lmdev_admin;