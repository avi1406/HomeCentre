CREATE OR REPLACE FUNCTION public.taskstepinstance(
	_id integer,
	_usercomments character varying)
    RETURNS taskstepinstance
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
BEGIN
    UPDATE taskstepinstance set 
			Usercomments = _usercomments
			where id = _id;
END
$BODY$;

ALTER FUNCTION public.taskstepinstance(integer, character varying)
    OWNER TO lmdev_admin;
