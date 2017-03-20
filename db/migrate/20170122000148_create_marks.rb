class CreateMarks < ActiveRecord::Migration[5.0]
  def change
    create_table :marks do |t|
	  t.references :tank, index: true, null: false, foreign_key: true
	  t.references :player, index: true, null: false, foreign_key: true
	  
      t.timestamps
    end
	
	add_index :marks, [:tank_id, :player_id], :unique => true
  end
end
