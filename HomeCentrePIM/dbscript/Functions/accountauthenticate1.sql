CREATE OR REPLACE FUNCTION public.accountauthenticate(
	_code integer)
    RETURNS TABLE(name character varying) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
 DECLARE 
     BEGIN
    RETURN QUERY
    
        SELECT 
             a.name
        FROM role a
        WHERE a.id = _code;
END; 
$BODY$;

ALTER FUNCTION public.accountauthenticate(integer)
    OWNER TO lmdev_admin;
