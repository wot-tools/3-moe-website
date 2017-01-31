class CreateTanks < ActiveRecord::Migration[5.0]
  def change
    create_table :tanks do |t|
      t.boolean :ispremium
      t.string :name
      t.string :shortname
      t.integer :tier
      t.string :bigicon
      t.string :contouricon
      t.string :smallicon
	  t.references :nation, type: :string, index: true, foreign_key: true
	  t.references :vehicle_type, type: :string, index: true, foreign_key: true

      t.timestamps
    end
	
	change_column :tanks, :id, :integer
  end
end
