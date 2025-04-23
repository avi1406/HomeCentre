CREATE OR REPLACE PROCEDURE public.appuser(
	)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT email FROM appuser WHERE id = 1) THEN
  UPDATE appuser SET email = 'admin@joyitsolutions.co' WHERE id = 1;
ELSE
  INSERT INTO appuser (email, name) VALUES ('admin@joyitsolutions.co', 'admin PIM');
END IF;
END;
$BODY$;

ALTER PROCEDURE public.appuser()
    OWNER TO lmdev_admin;