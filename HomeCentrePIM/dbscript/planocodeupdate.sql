CREATE OR REPLACE PROCEDURE public.planocodeupdate(
	)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    UPDATE public.uidinitiation a 
    SET planocode=b.uda_value, rmsintegrationdate = CURRENT_TIMESTAMP
    FROM rmsstaging b 
    WHERE a.planogram=b.uda_value_desc and a.planocode is null;

    UPDATE public.uidinitiation a 
    SET planocode=b.uda_value, rmsintegrationdate = CURRENT_TIMESTAMP
    FROM rmsstaging b 
    WHERE a.familyname=b.uda_value_desc and a.planocode is null;
END;
$BODY$;

ALTER PROCEDURE public.planocodeupdate()
    OWNER TO lmdev_admin;