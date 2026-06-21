extends Node2D

func _ready() -> void:
	var args := OS.get_cmdline_args()
		
	if args.has("--server"):
		DisplayServer.window_set_title("Protocol Server")
		get_tree().change_scene_to_file.call_deferred("res://test_server.tscn")
	else:
		DisplayServer.window_set_title("Protocol Client")
		get_tree().change_scene_to_file.call_deferred("res://test_client.tscn")
