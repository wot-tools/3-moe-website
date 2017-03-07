class CreatePlayers < ActiveRecord::Migration[5.0]
  def change
    create_table :players do |t|
      t.string :name, null: false
      t.integer :battles, null: false, default: 0
	  t.integer :wn8, null: false, default: 0
      t.integer :wgrating, null: false, default: 0
	  t.float :winratio, null: false, default: 0
	  t.integer :mark_count, null: false, default: 0
	  t.integer :moe_rating, null: false, default: 0
      t.datetime :lastLogout
      t.datetime :lastBattle
      t.datetime :accountCreated
      t.datetime :updatedAtWG      
      t.string :clientLang, null: false
	  t.references :clan, index: true, foreign_key: true

      t.timestamps
    end
	
	change_column :players, :id, :integer
  end
end
