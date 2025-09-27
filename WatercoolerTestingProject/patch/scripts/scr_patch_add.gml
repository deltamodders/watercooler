//WATERCOOLER// CREATE SCRIPT scr_patched_added
function scr_watercooler_demo() {
    draw_text(120, 160, "scr_watercooler_demo() added by WATERCOOLER");
    draw_sprite(spr_tenna, 0, 0, 200);
    draw_sprite(spr_lancer, 0, sprite_get_width(spr_tenna) + 12, 200);
    // This is stupid but idc
    if (!instance_exists(obj_watercooler))
        instance_create_layer(room_width / 2, 40, "Instances", obj_watercooler);
}