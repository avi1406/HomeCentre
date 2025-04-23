CREATE OR REPLACE FUNCTION public.accountauthenticate(
	)
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
        FROM appuser a;
END; 
$BODY$;

ALTER FUNCTION public.accountauthenticate()
    OWNER TO lmdev_admin;