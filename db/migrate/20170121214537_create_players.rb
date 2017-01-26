class CreatePlayers < ActiveRecord::Migration[5.0]
  def change
    create_table :players do |t|
      t.string :name
      t.integer :accountdbid
      t.integer :battles
      t.integer :wgrating
      t.datetime :lastLogout
      t.integer :winratio
      t.datetime :lastBattle
      t.datetime :accountCreated
      t.datetime :updatedAt
      t.integer :wn8
      t.string :clientLang
	  t.references :clan, index: true, foreign_key: true

      t.timestamps
    end
  end
end
