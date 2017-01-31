class CreatePlayers < ActiveRecord::Migration[5.0]
  def change
    create_table :players do |t|
      t.string :name
      t.integer :battles
      t.integer :wgrating
	  t.float :winratio
      t.datetime :lastLogout
      t.datetime :lastBattle
      t.datetime :accountCreated
      t.datetime :updatedAtWG
      t.integer :wn8
      t.string :clientLang
	  t.references :clan, index: true, foreign_key: true

      t.timestamps
    end
	
	change_column :players, :id, :integer
  end
end
